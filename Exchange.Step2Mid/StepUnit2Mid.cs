using System.Collections.Frozen;
using Exchange.Midlayer;
using StepCodeDotNet.Base;
using StepCodeDotNet.Gen.config_control_design;

namespace Exchange.Step2Mid;

public class StepUnit2Mid(Dictionary<int, IStepObj> stepIdObjMap)
{
    public void ResolveUnit(shape_representation shapeRep, MidMgr midMgr)
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
        if (stepIdObjMap.TryGetValue(id, out var obj) && obj is uncertainty_measure_with_unit measureWithUinit)
        {
            if (measureWithUinit.value_component is not length_measure value)
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
        var unit = Enum.Parse<SI_UNIT_NAME>(unitExpress.Value);
        if (unit != SI_UNIT_NAME.METRE)
        {
            return Unit.DEFAULT_LEN_FACTOR;
        }
        var prefix = Enum.Parse<SI_PREFIX>(prefixExpress.Value);
        return SiPrefixToFactor(prefix);

    }
    static double SiPrefixToFactor(SI_PREFIX prefix) => prefix switch
    {
        SI_PREFIX.EXA => 1e18,
        SI_PREFIX.PETA => 1e15,
        SI_PREFIX.TERA => 1e12,
        SI_PREFIX.GIGA => 1e9,
        SI_PREFIX.MEGA => 1e6,
        SI_PREFIX.KILO => 1e3,
        SI_PREFIX.HECTO => 1e2,
        SI_PREFIX.DECA => 1e1,
        SI_PREFIX.DECI => 1e-1,
        SI_PREFIX.CENTI => 1e-2,
        SI_PREFIX.MILLI => 1e-3,
        SI_PREFIX.MICRO => 1e-6,
        SI_PREFIX.NANO => 1e-9,
        SI_PREFIX.PICO => 1e-12,
        SI_PREFIX.FEMTO => 1e-15,
        SI_PREFIX.ATTO => 1e-18,
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
