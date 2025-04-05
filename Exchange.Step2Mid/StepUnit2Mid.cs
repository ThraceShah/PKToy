using System.Collections.Frozen;
using Exchange.Midlayer;
using StepCodeDotNet.Base;
using StepCodeDotNet.Gen.ap203;

namespace Exchange.Step2Mid;

public class StepUnit2Mid(Dictionary<int, IStepObj> stepIdObjMap)
{
    public void ResolveUnit(IShape_representation shapeRep, MidMgr midMgr)
    {
        var parts = new List<IBodyObj>();
        foreach (var item in shapeRep.items)
        {
            if (midMgr.ContainsMidObj(item.line_id) == false)
            {
                continue;
            }
            var midObj = midMgr.GetMidObjByImp<IMidObj>(item.line_id);
            if (midObj is IBodyObj midBody)
            {
                parts.Add(midBody);
            }
        }
        if (parts.Count == 0)
        {
            return;
        }
        if (shapeRep.context_of_items is not StepComplex complex)
        {
            return;
        }
        var unit = GetContexBaseUnit(complex);
        foreach (var part in parts)
        {
            part.Unit = unit;
        }
    }

    private Unit GetContexBaseUnit(StepComplex complex)
    {
        Unit r = new();
        EntityExpress? unitExpress = default;
        EntityExpress? toleranceExpress = default;
        foreach (var item in complex.complex)
        {
            if (item is not EntityExpress entityExpress)
            {
                Console.WriteLine($"GetContexBaseUnit: {item} is not EntityExpress.");
                continue;
            }
            if (entityExpress.EntityName == "GLOBAL_UNCERTAINTY_ASSIGNED_CONTEXT")
            {
                toleranceExpress = entityExpress;
                continue;
            }
            else if (entityExpress.EntityName == "GLOBAL_UNIT_ASSIGNED_CONTEXT")
            {
                unitExpress = entityExpress;
                continue;
            }
        }
        r = GetUnit(unitExpress);
        r.Tolerance = GetToleranceFactor(toleranceExpress);
        return r;
    }

    private double GetToleranceFactor(EntityExpress? express)
    {
        if (express is null)
        {
            return Unit.DEFAULT_TOLERANCE;
        }
        if (express.Args.Count == 0)
        {
            return Unit.DEFAULT_TOLERANCE;
        }
        if (express.Args[0] is not ListExpress listExpress)
        {
            return Unit.DEFAULT_TOLERANCE;
        }
        if (listExpress.ExpressList.Count == 0)
        {
            return Unit.DEFAULT_TOLERANCE;
        }
        if (listExpress.ExpressList[0] is not RefExpress refExpress)
        {
            Console.WriteLine($"GetToleranceFactor: {listExpress.ExpressList[0]} is not RefExpress.");
            return Unit.DEFAULT_TOLERANCE;
        }
        var id = refExpress.RefLineNumber;
        if (stepIdObjMap.TryGetValue(id, out var obj) && obj is IUncertainty_measure_with_unit measureWithUinit)
        {
            if (measureWithUinit.value_component is not REAL value)
            {
                Console.WriteLine($"GetToleranceFactor: {id} is not REAL.");
                return Unit.DEFAULT_TOLERANCE;
            }
            return value;
        }
        else
        {
            Console.WriteLine($"GetToleranceFactor: {id} not found in stepIdObjMap or not IUncertainty_measure_with_unit.");
            return Unit.DEFAULT_TOLERANCE;
        }

    }
    private Unit GetUnit(EntityExpress? express)
    {
        if (express is null)
        {
            return new Unit();
        }
        var unit = new Unit();
        if (express.Args.Count == 0)
        {
            return unit;
        }
        if (express.Args[0] is not ListExpress listExpress)
        {
            return unit;
        }
        foreach (var item in listExpress.ExpressList)
        {
            if (item is RefExpress refExpress)
            {
                var id = refExpress.RefLineNumber;
                if (stepIdObjMap.TryGetValue(id, out var obj) && obj is StepComplex complex)
                {
                    var complexType = GetComplexUnitType(complex);
                    switch (complexType)
                    {
                        case ComplexUnitType.LENGTH_UNIT:
                            unit.LengthFactor = GetComplexLengthFactor(complex);
                            break;
                        case ComplexUnitType.PLANE_ANGLE_UNIT:
                            unit.RadianFactor = GetComplexAngleFactor(complex);
                            break;
                        case ComplexUnitType.SOLID_ANGLE_UNIT:
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    Console.WriteLine($"GetUnit: {id} not found in stepIdObjMap or not StepComplex.");
                }
            }
            else
            {
                Console.WriteLine($"GetUnit: {item} is not RefExpress.");
            }
        }
        return unit;
    }

    private static ComplexUnitType GetComplexUnitType(StepComplex complex)
    {
        if (complex.complex.Any(x => x is EntityExpress entity && entity.EntityName == "LENGTH_UNIT"))
        {
            return ComplexUnitType.LENGTH_UNIT;
        }
        else if (complex.complex.Any(x => x is EntityExpress entity && entity.EntityName == "PLANE_ANGLE_UNIT"))
        {
            return ComplexUnitType.PLANE_ANGLE_UNIT;
        }
        else if (complex.complex.Any(x => x is EntityExpress entity && entity.EntityName == "SOLID_ANGLE_UNIT"))
        {
            return ComplexUnitType.SOLID_ANGLE_UNIT;
        }
        else
        {
            return ComplexUnitType.None;
        }
    }

    private static double GetComplexAngleFactor(StepComplex complex)
    {
        const double DEGREE_TO_RADIAN = double.Pi / 180.0;
        if (complex.complex.Any(x => x is EntityExpress entity && entity.EntityName == "CONVERSION_BASED_UNIT"))
        {
            return DEGREE_TO_RADIAN;
        }
        else
        {
            return Unit.DEFAULT_RADIAN_FACTOR;
        }
    }

    private static double GetComplexLengthFactor(StepComplex complex)
    {
        var express = complex.complex.Where(x => x is EntityExpress entity && entity.EntityName == "SI_UNIT").FirstOrDefault();
        if (express is not EntityExpress entityExpress)
        {
            return Unit.DEFAULT_LEN_FACTOR;
        }
        if (entityExpress.Args.Count != 2)
        {
            return Unit.DEFAULT_LEN_FACTOR;
        }
        if (entityExpress.Args[0] is not EnumExpress prefixExpress)
        {
            return Unit.DEFAULT_LEN_FACTOR;
        }
        if (entityExpress.Args[1] is not EnumExpress unitExpress)
        {
            return Unit.DEFAULT_LEN_FACTOR;
        }
        var unit = (si_unit_name)StepObjCreator.Instance.ToEnum(unitExpress.Value);
        if (unit != si_unit_name.metre)
        {
            return Unit.DEFAULT_LEN_FACTOR;
        }
        var prefix = (si_prefix)StepObjCreator.Instance.ToEnum(prefixExpress.Value);
        return SiPrefixToFactor(prefix);

    }
    static double SiPrefixToFactor(si_prefix prefix) => prefix switch
    {
        si_prefix.exa => 1e18,
        si_prefix.peta => 1e15,
        si_prefix.tera => 1e12,
        si_prefix.giga => 1e9,
        si_prefix.mega => 1e6,
        si_prefix.kilo => 1e3,
        si_prefix.hecto => 1e2,
        si_prefix.deca => 1e1,
        si_prefix.deci => 1e-1,
        si_prefix.centi => 1e-2,
        si_prefix.milli => 1e-3,
        si_prefix.micro => 1e-6,
        si_prefix.nano => 1e-9,
        si_prefix.pico => 1e-12,
        si_prefix.femto => 1e-15,
        si_prefix.atto => 1e-18,
        _ => 1.0
    };
    enum ComplexUnitType
    {
        None,
        LENGTH_UNIT,
        PLANE_ANGLE_UNIT,
        SOLID_ANGLE_UNIT
    }
}
