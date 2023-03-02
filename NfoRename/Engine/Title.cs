using Funcky;

namespace NfoRename.Engine;

[DiscriminatedUnion]
internal abstract partial record Title
{
    internal partial record SingleEpisode(string Title) : Title;
    internal partial record EpisodeWithPart(string Title, int Part) : Title;
}