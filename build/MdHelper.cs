// -----------------------------------------------------------------------
//  <copyright file="MdHelper.cs" company="Akka.NET Project">
//      Copyright 2021 Maintainers of NUKE.
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//      Distributed under the MIT License.
//      https://github.com/nuke-build/nuke/blob/master/LICENSE
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using NuGet.Versioning;
using Nuke.Common;
using Nuke.Common.ChangeLog;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Utilities;
using Serilog;

public static class MdHelper
{
    public static string GetNuGetReleaseNotes(string changelogFile, GitRepository repository = null)
    {
        var changelogSectionNotes = ExtractChangelogSectionNotes(changelogFile).ToList();

        if (repository.IsGitHubRepository())
        {
            changelogSectionNotes.Add(string.Empty);
            changelogSectionNotes.Add($"Full changelog at {repository.GetGitHubBrowseUrl(changelogFile)}");
        }

        return changelogSectionNotes.JoinNewLine();
    }
    
    /// <summary>
    /// Extracts the notes of the specified changelog section.
    /// </summary>
    /// <param name="changelogFile">The path to the changelog file.</param>
    /// <param name="tag">The tag which release notes should get extracted.</param>
    /// <returns>A collection of the release notes.</returns>
    [Pure]
    public static IEnumerable<string> ExtractChangelogSectionNotes(string changelogFile, string tag = null)
    {
        var content = TextTasks.ReadAllLines(changelogFile).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var sections = GetReleaseSections(content);
        var section = tag == null
            ? sections.First(x => x.StartIndex < x.EndIndex)
            : sections.First(x => x.Caption.EqualsOrdinalIgnoreCase(tag)).NotNull($"Could not find release section for '{tag}'.");

        return content
            .Skip(section.StartIndex + 1)
            .Take(section.EndIndex - section.StartIndex);
    }
    
    /// <summary>
    /// Reads the specified changelog.
    /// </summary>
    /// <param name="changelogFile">The path to the changelog file.</param>
    /// <returns>A <see cref="ChangeLog"/> object to work with the changelog.</returns>
    [Pure]
    public static ChangeLog ReadChangelog(string changelogFile)
    {
        var releaseNotes = ReadReleaseNotes(changelogFile);
        var unreleased = releaseNotes.Where(x => x.Unreleased).ToArray();

        if (unreleased.Length > 0)
        {
            Assert.True(unreleased.Length == 1, "Changelog should have only one draft section");
            return new ChangeLog(changelogFile, unreleased.First(), releaseNotes);
        }

        Assert.True(releaseNotes.Count(x => !x.Unreleased) >= 1, "Changelog should have at lease one released version section");
        return new ChangeLog(changelogFile, releaseNotes);
    }

    /// <summary>
    /// Reads the release notes from the given changelog file and returns the result.
    /// </summary>
    /// <param name="changelogFile">The path to the changelog file.</param>
    /// <returns>A readonly list of the release sections contained in the changelog.</returns>
    [Pure]
    public static IReadOnlyList<ReleaseNotes> ReadReleaseNotes(string changelogFile)
    {
        var lines = TextTasks.ReadAllLines(changelogFile).ToList();
        var releaseSections = GetReleaseSections(lines).ToList();

        Assert.True(releaseSections.Any(), "Changelog should have at least one release note section");
        return releaseSections.Select(Parse).ToList().AsReadOnly();

        ReleaseNotes Parse(ReleaseSection section)
        {
            var releaseNotes = lines
                .Skip(section.StartIndex + 1)
                .Take(section.EndIndex - section.StartIndex)
                .ToList()
                .AsReadOnly();

            return NuGetVersion.TryParse(section.Caption, out var version)
                ? new ReleaseNotes(version, releaseNotes, section.StartIndex, section.EndIndex)
                : new ReleaseNotes(releaseNotes, section.StartIndex, section.EndIndex);
        }
    }
    
    private static IEnumerable<ReleaseSection> GetReleaseSections(List<string> content)
    {
        static bool IsReleaseHead(string str)
            => str.StartsWith("## ");

        static string GetCaption(string str)
            => str
                .TrimStart('#', ' ', '[')
                .Split(' ')
                .First()
                .TrimEnd(']');

        var index = 0;
        while (index < content.Count)
        {
            string caption;
            while (true)
            {
                var line = content[index];
                if (IsReleaseHead(line))
                {
                    caption = GetCaption(line);
                    break;
                }
                index++;
                if (index == content.Count)
                    yield break;
            }

            var startIndex = index;
            while (index < content.Count)
            {
                index++;
                string line = null;
                if (index < content.Count)
                    line = content[index];
                
                if (index == content.Count || IsReleaseHead(line))
                {
                    var releaseData = new ReleaseSection
                    {
                        Caption = caption,
                        StartIndex = startIndex,
                        EndIndex = index -1
                    };
                    Log.Verbose("Found section '{Caption}' [{Start}-{End}]", caption, index, releaseData.EndIndex);
                    
                    yield return releaseData;
                    break;
                }
            }
        }
    }
    
    [DebuggerDisplay("{" + nameof(Caption) + "} [{" + nameof(StartIndex) + "}-{" + nameof(EndIndex) + "}]")]
    private class ReleaseSection
    {
        public string Caption;
        public int StartIndex;
        public int EndIndex;
    }
}
