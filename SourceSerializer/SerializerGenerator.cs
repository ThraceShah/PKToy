using System;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SourceSerializer
{
    public interface ISourceSerilize
    {
        void Serialize(Stream stream);

        void DeSerialize(Stream stream);
    }


    [Generator]
    public class AutoNotifyGenerator : ISourceGenerator
    {
        private const string attributeText = @"
using System;
using System.Runtime.InteropServices;
namespace SourceSerializer
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class SourceSerializeAttribute : Attribute
    {
        public SourceSerializeAttribute()
        {
        }
    }
    internal interface ISourceSerilize
    {
        void Serialize(Stream stream);

        void DeSerialize(Stream stream);
    }
    internal static class Serializer
    {

        internal static void WriteArray<T>(this Stream stream, T[] collection)
        where T : unmanaged
        {
            unsafe
            {
                var l = collection.Length * sizeof(T);
                var lSpan = new Span<byte>(&l, sizeof(int));
                stream.Write(lSpan.ToArray(), 0, lSpan.Length);
                var cSpan = MemoryMarshal.Cast<T, byte>(collection);
                stream.Write(cSpan.ToArray(),0,cSpan.Length);
            }
        }


        internal static T[] ReadArray<T>(this Stream stream)
        where T : unmanaged
        {
            var length = stream.ReadT<int>();
            var buffer = new byte[length];
            stream.Read(buffer, 0, length);
            var cSpan = MemoryMarshal.Cast<byte, T>(buffer);
            return cSpan.ToArray();
        }


        internal static void WriteT<T>(this Stream stream, T t)
        where T : unmanaged
        {
            unsafe
            {
                var cSpan = new Span<byte>(&t, sizeof(T));
                stream.Write(cSpan.ToArray(), 0, cSpan.Length);
            }
        }


        internal static T ReadT<T>(this Stream stream)
        where T : unmanaged
        {
            unsafe
            {
                var byteLength = sizeof(T);
                var buffer = new byte[byteLength];
                stream.Read(buffer,0,byteLength);
                var cSpan = MemoryMarshal.Cast<byte, T>(buffer);
                return cSpan[0];
            }
        }

    }

}
";
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="context">初始化上下文</param>
        public void Initialize(GeneratorInitializationContext context)
        {
            // 注册一个语法接收器，会在每次生成时被创建
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        /// <summary>
        /// 源码生成
        /// </summary>
        /// <param name="context">源码生成上下文</param>
        public void Execute(GeneratorExecutionContext context)
        {
            // 添加 Attrbite 文本
            context.AddSource("SourceSerializeAttribute", SourceText.From(attributeText, Encoding.UTF8));

            // 获取先前的语法接收器 
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            //// 创建出目标名称的属性
            CSharpParseOptions options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            Compilation compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(attributeText, Encoding.UTF8), options));

            //var compilation=context.Compilation;
            // 获取新绑定的 Attribute，并获取INotifyPropertyChanged
            INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName("SourceSerializer.SourceSerializeAttribute");
            INamedTypeSymbol notifySymbol = compilation.GetTypeByMetadataName("SourceSerializer.ISourceSerilize");


            // 遍历类型，只保留有 AutoNotify 标注的类型
            List<INamedTypeSymbol> typeSymbols = new List<INamedTypeSymbol>();
            foreach (TypeDeclarationSyntax candidateType in receiver.CandidateTypes)
            {
                SemanticModel model = compilation.GetSemanticModel(candidateType.SyntaxTree);
                // 获取字段符号信息，如果有 AutoNotify 标注则保存
                if (model.GetDeclaredSymbol(candidateType) is INamedTypeSymbol typeSymbol)
                {
                    if (typeSymbol.GetAttributes().Any(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default)))
                    {
                        typeSymbols.Add(typeSymbol);
                    }
                }
            }
            foreach (var typeSymbol in typeSymbols)
            {
                string classSource = ProcessClass(typeSymbol, attributeSymbol, notifySymbol, context);
                context.AddSource($"{typeSymbol.Name}Serializer", SourceText.From(classSource, Encoding.UTF8));
            }
        }

        private string ProcessClass(INamedTypeSymbol classSymbol, 
            ISymbol attributeSymbol, ISymbol notifySymbol, GeneratorExecutionContext context)
        {


            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, 
                SymbolEqualityComparer.Default))
            {
                // TODO: 必须在顶层，产生诊断信息
                return null;
            }
            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            // 开始构建要生成的代码
            StringBuilder source = new StringBuilder($@"
using static SourceSerializer.Serializer;
namespace {namespaceName}
{{
    public partial class {classSymbol.Name} : {notifySymbol.ToDisplayString()}
    {{
");

            // 如果类型还没有实现 INotifyPropertyChanged 则添加实现
            if (!classSymbol.Interfaces.Contains(notifySymbol, SymbolEqualityComparer.Default))
            {
                ProcessInterface(source, classSymbol);
            }

            source.Append("} }");
            return FormatCode(source.ToString());

        }

        /// <summary>
        /// 生成ISourceSerialize接口实现代码
        /// </summary>
        /// <param name="source">源码</param>
        /// <param name="classSymbol">类型符号</param>
        private void ProcessInterface(StringBuilder source, INamedTypeSymbol classSymbol)
        {
            var members= classSymbol.GetMembers();
            source.AppendLine($"public void Serialize(Stream stream)");
            source.AppendLine("{");

            // 获取类中的所有字段
            foreach (var member in members)
            {
                if (member is IFieldSymbol fieldSymbol)
                {
                    // 为每个字段生成序列化代码
                    ProcessSerialize(source, fieldSymbol);
                }
            }
            source.AppendLine("}");

            source.AppendLine($"public void DeSerialize(Stream stream)");
            source.AppendLine("{");

            // 获取类中的所有字段
            foreach (var member in members)
            {
                if (member is IFieldSymbol fieldSymbol)
                {
                    // 为每个字段生成序列化代码
                    ProcessDeSerialize(source, fieldSymbol);
                }
            }
            source.AppendLine("}");
        }

        /// <summary>
        /// 针对指定字段生成序列化代码
        /// </summary>
        /// <param name="source">源码</param>
        /// <param name="fieldSymbol">要序列化的字段</param>
        private void ProcessSerialize(StringBuilder source, IFieldSymbol fieldSymbol)
        {
            if (fieldSymbol.IsStatic)
            {
                return;
            }

            var fieldType = fieldSymbol.Type;
            // 检查字段类型是否为unmanaged
            if (fieldType.IsUnmanagedType)
            {
                // 对unmanaged类型直接序列化
                source.AppendLine($"stream.WriteT({fieldSymbol.Name});    // TODO: Serialize unmanaged type {fieldSymbol.Name}");
            }
            else if (ImplementsInterface(fieldType, "ISourceSerialize"))
            {
                // 对实现了ISourceSerialize接口的函数,执行Serialize方法
                source.AppendLine($"{fieldSymbol.Name}.Serialize(stream);    // TODO: Serialize type {fieldSymbol.Name} implementing ISourceSerialize");
            }
            else if (fieldType is IArrayTypeSymbol arrayType)
            {
                var elementType = arrayType.ElementType;

                if (elementType.IsUnmanagedType)
                {
                    // 对unmanaged类型的数组直接序列化
                    source.AppendLine($"stream.WriteArray({fieldSymbol.Name});    // TODO: Serialize array {fieldSymbol.Name} of unmanaged type");
                }
                //else if (ImplementsInterface(elementType, "ISourceSerialize"))
                else
                {
                    // 其余的数组假设他都实现了ISourceSerialize,执行Serialize方法,将来再完善
                    source.AppendLine($"stream.WriteT({fieldSymbol.Name}.Length);");
                    source.AppendLine($"foreach(var item in {fieldSymbol.Name})");
                    source.AppendLine("{");
                    source.AppendLine("item.Serialize(stream);");
                    source.AppendLine("}");
                }
            }
        }

        /// <summary>
        /// 针对指定字段生成反序列化代码
        /// </summary>
        /// <param name="source">源码</param>
        /// <param name="fieldSymbol">要反序列化的字段</param>
        private void ProcessDeSerialize(StringBuilder source, IFieldSymbol fieldSymbol)
        {
            if (fieldSymbol.IsStatic)
            {
                return;
            }

            var fieldType = fieldSymbol.Type;
            source.AppendLine("{");
            // 检查字段类型是否为unmanaged
            if (fieldType.IsUnmanagedType)
            {
                // 对unmanaged类型直接反序列化
                source.AppendLine($"this.{fieldSymbol.Name}=stream.ReadT<{fieldSymbol.Type.ToDisplayString()}>();    // TODO: Serialize unmanaged type {fieldSymbol.Name}");
            }
            else if (ImplementsInterface(fieldType, "ISourceSerialize"))
            {
                // 对实现了ISourceSerialize接口的函数,执行DeSerialize方法
                source.AppendLine($"this.{fieldSymbol.Name}=new {fieldSymbol.Type.ToDisplayString()}();");
                source.AppendLine($"this.{fieldSymbol.Name}.DeSerialize(stream);    // TODO: Serialize type {fieldSymbol.Name} implementing ISourceSerialize");
            }
            else if (fieldType is IArrayTypeSymbol arrayType)
            {
                var elementType = arrayType.ElementType;

                if (elementType.IsUnmanagedType)
                {
                    // 对unmanaged类型的数组直接序列化
                    source.AppendLine($"this.{fieldSymbol.Name} = stream.ReadArray<{elementType.ToDisplayString()}>();");
                }
                //else if (ImplementsInterface(elementType, "ISourceSerialize"))
                else
                {
                    // 其余的数组假设他都实现了ISourceSerialize,执行DeSerialize方法,将来再完善
                    var str = $@"
                var l = stream.ReadT<int>();
                var parts = new {elementType.ToDisplayString()}[l];
                for (int i = 0; i < l; i++)
                {{
                    var ele=new {elementType.ToDisplayString()}();
                    ele.DeSerialize(stream);
                    parts[i] = ele;
                }}
                this.{fieldSymbol.Name}=parts;
                ";
                    source.AppendLine(str);
                }
            }
            source.AppendLine("}");
        }

        /// <summary>
        /// 判断类型是否实现了指定的接口
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="interfaceName">接口名字</param>
        /// <returns>实现了返回true</returns>
        private bool ImplementsInterface(ITypeSymbol type, string interfaceName)
        {
            foreach (var iface in type.AllInterfaces)
            {
                if (iface.Name == interfaceName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 格式化代码
        /// </summary>
        /// <param name="csCode">cs代码</param>
        /// <returns>格式化后的代码</returns>
        public string FormatCode(string csCode)
        {
            var tree = CSharpSyntaxTree.ParseText(csCode);
            var root = tree.GetRoot().NormalizeWhitespace();
            var ret = root.ToFullString();
            return ret;
        }


        // 语法接收器，将在每次生成代码时被按需创建
        class SyntaxReceiver : ISyntaxReceiver
        {
            public List<TypeDeclarationSyntax> CandidateTypes { get; } = new List<TypeDeclarationSyntax>();

            // 编译中在访问每个语法节点时被调用，我们可以检查节点并保存任何对生成有用的信息
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // 将具有至少一个 Attribute 的任何字段作为候选
                if (syntaxNode is TypeDeclarationSyntax fieldDeclarationSyntax
                    && fieldDeclarationSyntax.AttributeLists.Count > 0)
                {
                    CandidateTypes.Add(fieldDeclarationSyntax);
                }
            }
        }
    }
}
