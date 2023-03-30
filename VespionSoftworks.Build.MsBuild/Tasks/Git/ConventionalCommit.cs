using System.Collections.Generic;

namespace VespionSoftworks.Build.MsBuild.Tasks.Git;

public readonly record struct ConventionalCommit(string Sha, string Type, string Scope, bool IsBreaking, string Subject, string Body, IDictionary<string, string> Footers);