namespace AspireLove.Core.Tests;

public class AspireProjectLocatorTests
{
    [Fact]
    public void Returns_null_when_no_aspire_folder()
    {
        using var temp = new TempProject();
        Assert.Null(AspireProjectLocator.Locate(temp.Path));
    }

    [Fact]
    public void Returns_null_for_blank_path() =>
        Assert.Null(AspireProjectLocator.Locate("  "));

    [Fact]
    public void Locates_apphost_regardless_of_name()
    {
        using var temp = new TempProject();
        var appHostDir = Path.Combine(temp.Path, "aspire", "Renamed.AppHost");
        Directory.CreateDirectory(appHostDir);
        var csproj = Path.Combine(appHostDir, "Renamed.AppHost.csproj");
        File.WriteAllText(csproj, "<Project />");

        var info = AspireProjectLocator.Locate(temp.Path);

        Assert.NotNull(info);
        Assert.Equal(appHostDir, info!.AppHostDirectory);
        Assert.Equal(csproj, info.AppHostProjectPath);
    }

    [Fact]
    public void Returns_null_when_apphost_folder_has_no_matching_csproj()
    {
        using var temp = new TempProject();
        Directory.CreateDirectory(Path.Combine(temp.Path, "aspire", "Foo.AppHost"));

        Assert.Null(AspireProjectLocator.Locate(temp.Path));
    }
}
