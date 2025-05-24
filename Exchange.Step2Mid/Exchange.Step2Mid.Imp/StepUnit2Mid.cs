using System.Collections.Frozen;
using System.Xml;
using Exchange.Midlayer;

#if AP_VERSION_203
namespace Exchange.Step2Mid.Ap203;
#endif
#if AP_VERSION_203E2
namespace Exchange.Step2Mid.Ap203e2;
#endif
#if AP_VERSION_214E3
namespace Exchange.Step2Mid.Ap214e3;
#endif
internal class StepUnit2Mid(Dictionary<int, IStepObj> stepIdObjMap)
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
        if (shapeRep.context_of_items is not global_uncertainty_assigned_context_and_global_unit_assigned_context_imp unitContex)
        {
            return;
        }
        Unit unit = new();
        foreach (var uncertainty in unitContex.uncertainty)
        {
            if (uncertainty is uncertainty_measure_with_unit measureWithUinit)
            {
                unit.Tolerance = (length_measure)measureWithUinit.value_component;
            }
        }
        foreach (var stepUnit in unitContex.units)
        {
            if (stepUnit is si_unit_and_length_unit_imp lengthUnit)
            {
                unit.LengthFactor = SiPrefixToFactor(lengthUnit.prefix);
            }
            else if (stepUnit is conversion_based_unit_and_plane_angle_unit_imp angleUnit)
            {
                unit.RadianFactor = (plane_angle_measure)angleUnit.conversion_factor.value_component;
            }
        }
        foreach (var part in parts)
        {
            part.Unit = unit;
        }
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
}
