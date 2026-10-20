using ChemSculptor.Anomaly.Abstractions;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Anomaly.Registry;
using ChemSculptor.Skills.Common;
using Microsoft.Extensions.DependencyInjection;

namespace ChemSculptor.Core.Tests;

/// <summary>异常提供器注册表测试。</summary>
public class AnomalyProviderRegistryTests
{
    /// <summary>验证检查可以注册并列出。</summary>
    [Fact]
    public void RegistersAndListsCheck()
    {
        AnomalyProviderRegistry registry =
            new AnomalyProviderRegistry();
        TestAnomalyCheck check = new TestAnomalyCheck();

        registry.RegisterCheck(check);

        IReadOnlyList<IAnomalyCheck> checks =
            registry.ListChecks();

        Assert.Single(checks);
        Assert.Equal("test-anomaly", checks[0].Descriptor.Code);
    }

    /// <summary>验证重复注册不会被静默覆盖。</summary>
    [Fact]
    public void RejectsDuplicateCheckCode()
    {
        AnomalyProviderRegistry registry =
            new AnomalyProviderRegistry();
        registry.RegisterCheck(new TestAnomalyCheck());

        bool exceptionThrown = false;

        try
        {
            registry.RegisterCheck(new TestAnomalyCheck());
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        Assert.True(exceptionThrown);
    }

    /// <summary>验证通用服务注册不会把接口本身作为实现类型。</summary>
    [Fact]
    public void CommonRegistrationDoesNotUseInterfaceAsImplementation()
    {
        ServiceCollection services = new ServiceCollection();

        CommonSkillServiceRegistration.AddCommonSkills(services);

        for (int index = 0;
            index < services.Count;
            index++)
        {
            Type? implementationType =
                services[index].ImplementationType;

            Assert.False(
                implementationType != null
                && implementationType.IsInterface,
                "服务 " +
                services[index].ServiceType.FullName +
                " 把接口本身注册为实现类型。");
        }
    }

    private sealed class TestAnomalyCheck : IAnomalyCheck
    {
        public AnomalyCheckDescriptor Descriptor
        {
            get
            {
                AnomalyCheckDescriptor descriptor =
                    new AnomalyCheckDescriptor();
                descriptor.Code = "test-anomaly";
                descriptor.DisplayName = "测试异常";
                descriptor.Category = AnomalyCategory.Scientific;
                descriptor.Mechanism =
                    AnomalyCheckMechanism.OutputArtifact;
                return descriptor;
            }
        }

        public bool CanCheck(AnomalyContext context)
        {
            return true;
        }

        public Task<AnomalyCheckResult> CheckAsync(
            AnomalyContext context,
            CancellationToken cancellationToken = default)
        {
            AnomalyCheckResult result = new AnomalyCheckResult();
            result.Code = "test-anomaly";
            result.Status = AnomalyCheckStatus.Passed;
            return Task.FromResult(result);
        }
    }
}
