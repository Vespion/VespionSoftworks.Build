using System;
using System.IO;
using JetBrains.Annotations;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace VespionSoftworks.Build.MsBuild.Tasks.Git;

public class LocateRepository: Task
{
	[PublicAPI, Required] public string ProjectPath { get; set; } = null!;

	[PublicAPI, Output] public string RepositoryRoot { get; set; } = null!;
	
	/// <inheritdoc />
	public override bool Execute()
	{
		var root = Path.GetPathRoot(ProjectPath);
		var path = ProjectPath;
		do
		{
			Log.LogMessage(MessageImportance.Low, "Checking {0} for .git directory", path);
			if (Directory.Exists(Path.Combine(path, ".git")))
			{
				RepositoryRoot = path;
				Log.LogMessage(MessageImportance.Normal, "Repository root located at {0}", RepositoryRoot);
				return true;
			}

			var parent = Path.GetDirectoryName(path);
			if (parent == null)
			{
				break;
			}

			path = parent;
		} while (path != root);
		
		Log.LogWarning("Could not locate repository root");
		return false;
	}
}