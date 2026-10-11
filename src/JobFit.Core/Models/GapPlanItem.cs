namespace JobFit.Models;

/// <summary>The kind of action suggested for a missing skill.</summary>
public enum GapAction
{
    /// <summary>The skill looks new: learn it and build something small.</summary>
    Learn,
    /// <summary>A related skill is already on the resume: point to that project.</summary>
    ShowProject,
    /// <summary>Probably a wording gap: describe existing experience in the posting's words.</summary>
    Reword
}

/// <summary>One actionable line of the gap plan.</summary>
public record GapPlanItem(string Skill, GapAction Action, string Advice);
