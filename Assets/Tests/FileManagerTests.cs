using System;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class FileManagerTests
{
    private GameObject fileManagerObj;
    private Component fileManager;
    private string testFilePath;

    [SetUp]
    public void Setup()
    {
        fileManagerObj = new GameObject("File Manager");

        Type fileManagerType = GetTypeFromAssembly("FileManager");
        fileManager = fileManagerObj.AddComponent(fileManagerType);

        testFilePath = Path.Combine(Application.streamingAssetsPath, "FileManagerTest.txt");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(testFilePath))
        {
            File.Delete(testFilePath);
        }

        UnityEngine.Object.DestroyImmediate(fileManagerObj);
    }

    [Test]
    public void ReadFile_WhenFileExists_ReturnsFileContent()
    {
        File.WriteAllText(testFilePath, "COMP3850Test");

        string content = (string)fileManager.GetType()
            .GetMethod("ReadFile")
            .Invoke(fileManager, new object[] { testFilePath });

        Assert.AreEqual("COMP3850Test", content);
    }

    [Test]
	public void ReadFile_WhenFileDoesNotExist_ReturnsEmptyString()
	{
		if(File.Exists(testFilePath))
		{
			File.Delete(testFilePath);
		}

        LogAssert.Expect(LogType.Error, $"ERROR: File not found at: \"{testFilePath}\"!");

		string result = (string)fileManager.GetType()
			.GetMethod("ReadFile")
			.Invoke(fileManager, new object[] { testFilePath });

		Assert.AreEqual("", result);
	}

    [Test]
    public void WriteFile_WhenOverwriteIsTrue_OverwritesExistingFile()
    {
        File.WriteAllText(testFilePath, "COMP3850OldTest");

        fileManager.GetType()
            .GetMethod("WriteFile")
            .Invoke(fileManager, new object[] { testFilePath, "COMP3850NewTest", true });

        string content = File.ReadAllText(testFilePath);
        
        Assert.AreEqual("COMP3850NewTest", content);
    }

    [Test]
    public void WriteFile_WhenOverwriteIsFalse_DoesNotOverwriteExistingFile()
    {
        File.WriteAllText(testFilePath, "COMP3850OldTest");

        fileManager.GetType()
            .GetMethod("WriteFile")
            .Invoke(fileManager, new object[] { testFilePath, "COMP3850NewTest", false });

        string content = File.ReadAllText(testFilePath);

        Assert.AreEqual("COMP3850OldTest", content);
    }

	private Type GetTypeFromAssembly(string typeName)
	{
		foreach(System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			Type type = assembly.GetType(typeName);

			if(type != null)
			{
				return type;
			}
		}

		Assert.Fail("Could not find: " + typeName);
		return null;
	}
}