namespace MeloongCore.Tests;

public abstract class TestBase {

    public TestBase() {
        MeloongCore.Main.Init("MeloongCoreTest");
        Logger.Instance = new TestLogger();
    }
    private class TestLogger : BaseLogger {
        public override void HandleBehavior(string? rawMessage, string formattedMessage, LogBehavior behavior, Exception? ex)
            => TestContext.Current!.OutputWriter.WriteLine(formattedMessage);
    }

}

public abstract class TestWithFolder : TestBase {

    public readonly string tempFolder;
    public TestWithFolder() : base() {
        tempFolder = Path.Combine(
            Path.GetTempPath(), "PCL", "Tests",
            $"{GetType().Name}-{Guid.NewGuid()}",
            "文件夹 Dir_!@#$%^&()_+={}[];',_",
            new string('X', 200), new string('X', 200)) + @"\";
        DirectoryUtils.Create(tempFolder);
    }

    public string GetTestFile(string fileName) {
        var sourceFilePath = Path.Combine(Paths.Base, "TestFiles", GetType().Name.Replace("Test", ""), fileName);
        var distPath = Path.Combine(tempFolder, PathUtils.GetLastPart(sourceFilePath));
        FileUtils.Copy(sourceFilePath, distPath);
        return distPath;
    }

}
