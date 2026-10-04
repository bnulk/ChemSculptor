using ChemSculptor.FundamentalConstants.Chemistry.Elements;
using ChemSculptor.FundamentalConstants.Physics;
using ChemSculptor.FundamentalConstants.Units;

namespace ChemSculptor.Core.Tests;

/// <summary>物理常数、单位换算和元素数据测试。</summary>
public class FundamentalConstantsTests
{
    /// <summary>验证摩尔气体常数由阿伏伽德罗常数和玻尔兹曼常数推导。</summary>
    [Fact]
    public void MolarGasConstantMatchesDefinition()
    {
        double expected =
            PhysicalConstants.AvogadroConstant
            * PhysicalConstants.BoltzmannConstant;

        Assert.Equal(PhysicalConstants.MolarGasConstant, expected);
    }

    /// <summary>验证常用哈特里能量换算值。</summary>
    [Fact]
    public void HartreeConversionsUseExpectedValues()
    {
        Assert.InRange(
            UnitConversions.HartreeToElectronVolts,
            27.2113862459,
            27.2113862461);

        Assert.InRange(
            UnitConversions.HartreeToWavenumbers,
            219474.63136,
            219474.63137);

        Assert.InRange(
            UnitConversions.BohrToAngstroms,
            0.5291772105,
            0.5291772106);
    }

    /// <summary>验证元素符号查询忽略大小写。</summary>
    [Fact]
    public void FindsElementBySymbolIgnoringCase()
    {
        ChemicalElement element;
        bool found = ElementCatalog.TryGetBySymbol("o", out element);

        Assert.True(found);
        Assert.Equal(8, element.AtomicNumber);
        Assert.Equal("O", element.Symbol);
        Assert.Equal("Oxygen", element.Name);
        Assert.Equal(
            AtomicMassKind.LegacyGeneralPurpose,
            element.AtomicMassKind);
        Assert.Equal("Migrated from ZhangCang", element.AtomicMassSource);
    }

    /// <summary>验证按原子序数查询元素。</summary>
    [Fact]
    public void FindsElementByAtomicNumber()
    {
        ChemicalElement element;
        bool found = ElementCatalog.TryGetByAtomicNumber(1, out element);

        Assert.True(found);
        Assert.Equal("H", element.Symbol);
        Assert.Equal("Hydrogen", element.Name);
    }

    /// <summary>验证未知元素不会伪装成成功查询。</summary>
    [Fact]
    public void ReturnsFalseForUnknownElement()
    {
        ChemicalElement element;
        bool found = ElementCatalog.TryGetBySymbol(
            "not-an-element",
            out element);

        Assert.False(found);
        Assert.Equal(0, element.AtomicNumber);
    }

    /// <summary>验证当前迁移数据覆盖虚拟元素和氢到氙。</summary>
    [Fact]
    public void CatalogContainsExpectedNumberRange()
    {
        Assert.Equal(55, ElementCatalog.All.Count);
        Assert.Equal(0, ElementCatalog.All[0].AtomicNumber);
        Assert.Equal(54, ElementCatalog.All[54].AtomicNumber);
    }
}
