using VmixScheduler;

namespace VmixScheduler.Tests;

public class VmixInputTests
{
    [Fact]
    public void Name_FallsBackToTitle_WhenShortTitleIsBlank()
    {
        var input = new VmixInput { Title = "Full Title.mp4", ShortTitle = "" };

        Assert.Equal("Full Title.mp4", input.Name);
    }

    [Fact]
    public void Name_UsesShortTitle_WhenSet()
    {
        var input = new VmixInput { Title = "Filler - song.mp4", ShortTitle = "Filler" };

        Assert.Equal("Filler", input.Name);
    }

    [Fact]
    public void CurrentSongTitle_ReturnsSelectedListItem_ExtensionStripped()
    {
        var input = new VmixInput
        {
            ListItems = new List<string> { @"C:\Media\song1.mp3", @"C:\Media\song2.mp3" },
            SelectedIndex = 0
        };

        Assert.Equal("song1", input.CurrentSongTitle);
    }

    [Fact]
    public void NextSongTitle_ReturnsItemAfterSelected()
    {
        var input = new VmixInput
        {
            ListItems = new List<string> { @"C:\Media\song1.mp3", @"C:\Media\song2.mp3" },
            SelectedIndex = 0
        };

        Assert.Equal("song2", input.NextSongTitle);
    }

    [Fact]
    public void NextSongTitle_Null_WhenSelectedIsLastItem()
    {
        var input = new VmixInput
        {
            ListItems = new List<string> { @"C:\Media\song1.mp3", @"C:\Media\song2.mp3" },
            SelectedIndex = 1
        };

        Assert.Null(input.NextSongTitle);
    }

    [Fact]
    public void CurrentSongTitle_Null_WhenSelectedIndexIsNegative()
    {
        var input = new VmixInput
        {
            ListItems = new List<string> { @"C:\Media\song1.mp3" },
            SelectedIndex = -1
        };

        Assert.Null(input.CurrentSongTitle);
    }

    [Fact]
    public void CurrentSongTitle_Null_WhenThereAreNoListItems()
    {
        var input = new VmixInput { ListItems = new List<string>(), SelectedIndex = -1 };

        Assert.Null(input.CurrentSongTitle);
    }

    [Fact]
    public void FileName_Null_WhenTitleEqualsShortTitle()
    {
        // Once a single-file input is renamed, vMix's title becomes identical to the rename and
        // there's no original file name left anywhere in the API.
        var input = new VmixInput { Title = "Now", ShortTitle = "Now" };

        Assert.Null(input.FileName);
    }

    [Fact]
    public void FileName_StripsListInputTitlePrefix()
    {
        var input = new VmixInput { Title = @"Filler - C:\Media\song1.mp3", ShortTitle = "Filler" };

        Assert.Equal("song1", input.FileName);
    }

    [Fact]
    public void FileName_ReturnsFileNameDirectly_WhenNoShortTitle()
    {
        var input = new VmixInput { Title = @"C:\Media\movie.mp4", ShortTitle = "" };

        Assert.Equal("movie", input.FileName);
    }

    [Fact]
    public void FileName_Null_WhenTitleIsBlank()
    {
        var input = new VmixInput { Title = "", ShortTitle = "" };

        Assert.Null(input.FileName);
    }

    [Fact]
    public void HasFinishedPlaying_False_WhenFirstItemOfMultiItemListIsNearEnd()
    {
        // Regression: Position/Duration reflect only the currently-playing item within a list, not
        // the list as a whole — a multi-item Program list must not be reported as "finished" just
        // because item 0 of 3 is about to end, or the auto-filler would cut away mid-list.
        var input = new VmixInput
        {
            Position = 4800,
            Duration = 5000,
            ListItems = new List<string> { @"C:\Media\clip1.mp4", @"C:\Media\clip2.mp4", @"C:\Media\clip3.mp4" },
            SelectedIndex = 0
        };

        Assert.False(input.HasFinishedPlaying());
    }

    [Fact]
    public void HasFinishedPlaying_True_WhenLastItemOfMultiItemListIsNearEnd()
    {
        var input = new VmixInput
        {
            Position = 4800,
            Duration = 5000,
            ListItems = new List<string> { @"C:\Media\clip1.mp4", @"C:\Media\clip2.mp4", @"C:\Media\clip3.mp4" },
            SelectedIndex = 2
        };

        Assert.True(input.HasFinishedPlaying());
    }

    [Fact]
    public void HasFinishedPlaying_True_ForSingleClipInput_WhenNearEnd()
    {
        var input = new VmixInput { Position = 4800, Duration = 5000, ListItems = new List<string>(), SelectedIndex = -1 };

        Assert.True(input.HasFinishedPlaying());
    }

    [Fact]
    public void HasFinishedPlaying_False_WhenNotNearEnd()
    {
        var input = new VmixInput { Position = 0, Duration = 5000, ListItems = new List<string>(), SelectedIndex = -1 };

        Assert.False(input.HasFinishedPlaying());
    }

    [Fact]
    public void HasFinishedPlaying_False_WhenDurationIsZero()
    {
        var input = new VmixInput { Position = 0, Duration = 0 };

        Assert.False(input.HasFinishedPlaying());
    }
}
