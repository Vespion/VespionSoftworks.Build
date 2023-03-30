using System.Collections.Generic;

namespace VespionSoftworks.Build.MsBuild.Tasks.Git;

public struct SemVer
{
	public int Major { get; private set; }
	
	public int Minor { get; private set; }
	
	public int Patch { get; private set; }
	
	public string Prerelease => string.Join(".", _prereleaseIdentifiers);
	private readonly List<string> _prereleaseIdentifiers;

	public SemVer()
	{
		_prereleaseIdentifiers = new List<string>();
		_metadata = new List<string>();
	}

	public string Metadata => string.Join(".", _metadata);
	private readonly List<string> _metadata;
	
	/// <inheritdoc />
	public override string ToString()
	{
		return
			$"{Major}.{Minor}.{Patch}{(string.IsNullOrEmpty(Prerelease) ? "" : $"-{Prerelease}")}{(string.IsNullOrEmpty(Metadata) ? "" : $"+{Metadata}")}";
	}

	public SemVer IncrementBreakingChange()
	{
		Major++;
		Minor = 0;
		Patch = 0;

		return this;
	}

	public SemVer IncrementMinor()
	{
		Minor++;
		Patch = 0;
		
		return this;
	}
	
	public SemVer IncrementPatch()
	{
		Patch++;
		
		return this;
	}
	
	public SemVer AddPrereleaseIdentifier(string identifier)
	{
		_prereleaseIdentifiers.Add(identifier);
		
		return this;
	}
	
	public SemVer AddMetadata(string metadata)
	{
		_metadata.Add(metadata);
		
		return this;
	}
}