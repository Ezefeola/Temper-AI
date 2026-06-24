using TemperAI.Installer;

namespace TemperAI.Installer.UnitTests;

public sealed class LocalModeArgumentsTests
{
    [Fact]
    public void Process_WithoutLocalFlag_ReturnsArgsUnchanged()
    {
        string[] args = ["install", "-a", "claude"];

        (bool localMode, string[] result) = LocalModeArguments.Process(args);

        Assert.False(localMode);
        Assert.Equal(args, result);
    }

    [Fact]
    public void Process_LocalInstall_InjectsSourceLocal()
    {
        (bool localMode, string[] result) = LocalModeArguments.Process(["--local", "install"]);

        Assert.True(localMode);
        Assert.Equal(["install", "--source", "local"], result);
    }

    [Fact]
    public void Process_LocalUpdate_PreservesOtherArgsAndInjectsSource()
    {
        (bool localMode, string[] result) = LocalModeArguments.Process(["--local", "update", "-a", "claude"]);

        Assert.True(localMode);
        Assert.Equal(["update", "-a", "claude", "--source", "local"], result);
    }

    [Fact]
    public void Process_LocalAlone_ReturnsEmptyArgsToTriggerMenu()
    {
        (bool localMode, string[] result) = LocalModeArguments.Process(["--local"]);

        Assert.True(localMode);
        Assert.Empty(result);
    }

    [Fact]
    public void Process_LocalInstallWithExplicitSource_DoesNotDuplicate()
    {
        (bool localMode, string[] result) = LocalModeArguments.Process(["--local", "install", "--source", "remote"]);

        Assert.True(localMode);
        Assert.Equal(["install", "--source", "remote"], result);
    }

    [Fact]
    public void Process_LocalWithNonSourceAwareCommand_DoesNotInjectSource()
    {
        (bool localMode, string[] result) = LocalModeArguments.Process(["--local", "status"]);

        Assert.True(localMode);
        Assert.Equal(["status"], result);
    }

    [Theory]
    [InlineData("install", true, "install --source local")]
    [InlineData("update", true, "update --source local")]
    [InlineData("status", true, "status")]
    [InlineData("install", false, "install")]
    public void SubcommandArgs_AppendsSourceOnlyForSourceAwareCommandsInLocalMode(
        string commandName,
        bool localMode,
        string expected)
    {
        Assert.Equal(expected, LocalModeArguments.SubcommandArgs(commandName, localMode));
    }
}
