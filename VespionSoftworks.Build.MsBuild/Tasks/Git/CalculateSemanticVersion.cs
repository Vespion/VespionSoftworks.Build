using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace VespionSoftworks.Build.MsBuild.Tasks.Git;

public partial class CalculateSemanticVersion: Task
{
	[Required, PublicAPI]
	public string RepositoryRoot { get; set; } = null!;
	
	[Required, PublicAPI]
	public string ProjectRoot { get; set; } = null!;
	
	[Required, PublicAPI]
	public string Head { get; set; } = null!;
	
	[PublicAPI]
	public string MainBranch { get; set; } = "main";
	
	[Required, PublicAPI]
	public string CurrentBranch { get; set; } = null!;

	[Output, PublicAPI]
	public string Major { get; set; } = null!;
	[Output, PublicAPI]
	public string Minor { get; set; } = null!;
	[Output, PublicAPI]
	public string Patch { get; set; } = null!;
	[Output, PublicAPI]
	public string Prerelease { get; set; } = null!;
	[Output, PublicAPI]
	public string Metadata { get; set; } = null!;
	
	public override bool Execute()
	{
		var version = new SemVer();

		var commitHash = Head;
		var commitCount = 0;
		foreach (var commit in GetCommits())
		{
			commitCount++;
			if (commit.IsBreaking)
			{
				version = version.IncrementBreakingChange();
				continue;
			}

			switch (commit.Type.ToLower())
			{
				case "docs":
				case "style":
				case "refactor":
				case "perf":
				case "test":
				case "build":
				case "ci":
				case "chore":
					break;
				case "fix":
					version = version.IncrementPatch();
					break;
				default:
					version = version.IncrementMinor();
					break;
			}

			commitHash = commit.Sha;
		}

		version = version.AddMetadata(commitHash);
		
		if (CurrentBranch != MainBranch)
		{
			version = version.AddPrereleaseIdentifier(CurrentBranch.Replace('/', '-'));
			version = version.AddPrereleaseIdentifier(commitCount.ToString());
		}
		
		Log.LogMessage(MessageImportance.High, $"Calculated version: {version}!");
		SetVersion(version);
		return true;
	}

	[GeneratedRegex("(?<type>.*?)(\\((?<scope>.*)\\))?(?<breaking>!)?: (?<subject>.*)", RegexOptions.ExplicitCapture)]
	private static partial Regex HeaderPattern();
    
	[GeneratedRegex("^(?<key>\\w*(-\\w*)*): (?<value>.*)|^(?<key>BREAKING CHANGE): (?<value>.*)", RegexOptions.ExplicitCapture)]
	private static partial Regex FooterPattern();

	private string GetCommitMessage(string hash)
	{
		var proc = new Process
		{
			StartInfo = new ProcessStartInfo("git")
			{
				Arguments = "log -n 1 --pretty=format:%s "+ hash,
				WorkingDirectory = RepositoryRoot,
				UseShellExecute = false,
				RedirectStandardOutput = true,
			}
		};
		
		proc.Start();

		proc.WaitForExit();

		return proc.StandardOutput.ReadToEnd();
	}

	private IEnumerable<string> GetCommitHashes()
	{
		var proc = new Process
		{
			StartInfo = new ProcessStartInfo("git")
			{
				Arguments = "log --reverse --pretty=format:%h -- " + ProjectRoot,
				WorkingDirectory = RepositoryRoot,
				UseShellExecute = false,
				RedirectStandardOutput = true,
			}
		};
		
		proc.Start();
		
		while (!proc.StandardOutput.EndOfStream)
		{
			yield return proc.StandardOutput.ReadLine()!;
		}
	}

	private IEnumerable<ConventionalCommit> GetCommits()
	{
		foreach (var commit in GetCommitHashes())
		{
			var lines = GetCommitMessage(commit).Split('\n');
			var headerMatch = HeaderPattern().Match(lines[0]);
			var sbBody = new StringBuilder();

			var footer = new Dictionary<string, string>();

			if (lines.Length > 1)
			{
				foreach (var s in lines.Skip(1))
				{
					if (string.IsNullOrWhiteSpace(s))
					{
						continue;
					}
				
					var footerMatch = FooterPattern().Match(s);
					if (footerMatch.Success)
					{
						footer[footerMatch.Groups["key"].Value] = footerMatch.Groups["value"].Value;
					}
					else
					{
						sbBody.AppendLine(s);
					}
				}
			}

			var isBreaking = footer.ContainsKey("BREAKING CHANGE") || footer.ContainsKey("BREAKING-CHANGE") ||
			                 headerMatch.Groups["breaking"].Success;

		
			var cCommit = new ConventionalCommit(commit, headerMatch.Groups["type"].Value, headerMatch.Groups["scope"].Value, isBreaking,
				headerMatch.Groups["subject"].Value, sbBody.ToString(), footer);
		
			yield return cCommit;
		}
	}

	private void SetVersion(SemVer semVer)
	{
		Major = semVer.Major.ToString();
		Minor = semVer.Minor.ToString();
		Patch = semVer.Patch.ToString();
		Prerelease = semVer.Prerelease;
		Metadata = semVer.Metadata;
	}
}