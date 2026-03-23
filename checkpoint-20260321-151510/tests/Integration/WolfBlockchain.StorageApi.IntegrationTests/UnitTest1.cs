using WolfBlockchain.Api.Abstractions;
using WolfBlockchain.Storage.Abstractions;

namespace WolfBlockchain.StorageApi.IntegrationTests;

public class StorageApiContractTests
{
    [Fact]
    public void PublicApiService_ShouldExposeExplicitApiResultContracts()
    {
        var submit = typeof(IPublicApiService).GetMethod(nameof(IPublicApiService.SubmitTransactionAsync));
        var getBlock = typeof(IPublicApiService).GetMethod(nameof(IPublicApiService.GetBlockByHashAsync));

        Assert.NotNull(submit);
        Assert.NotNull(getBlock);
        Assert.StartsWith("System.Threading.Tasks.ValueTask", submit!.ReturnType.FullName);
        Assert.StartsWith("System.Threading.Tasks.ValueTask", getBlock!.ReturnType.FullName);
    }

    [Fact]
    public void StorageRepositories_ShouldExposeCancellationTokenInAsyncMethods()
    {
        var saveBlock = typeof(IBlockStore).GetMethod(nameof(IBlockStore.SaveBlockAsync));
        var currentHeight = typeof(IChainReadRepository).GetMethod(nameof(IChainReadRepository.GetCurrentHeightAsync));

        Assert.NotNull(saveBlock);
        Assert.NotNull(currentHeight);
        Assert.Equal(typeof(CancellationToken), saveBlock!.GetParameters()[1].ParameterType);
        Assert.Equal(typeof(CancellationToken), currentHeight!.GetParameters()[0].ParameterType);
    }
}
