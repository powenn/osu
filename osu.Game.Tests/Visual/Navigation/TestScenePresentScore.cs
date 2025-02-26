// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Screens;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Osu;
using osu.Game.Scoring;
using osu.Game.Screens;
using osu.Game.Screens.Menu;
using osu.Game.Screens.Play;
using osu.Game.Screens.Ranking;

namespace osu.Game.Tests.Visual.Navigation
{
    public class TestScenePresentScore : OsuGameTestScene
    {
        private BeatmapSetInfo beatmap;

        [SetUpSteps]
        public new void SetUpSteps()
        {
            AddStep("import beatmap", () =>
            {
                var difficulty = new BeatmapDifficulty();
                var metadata = new BeatmapMetadata
                {
                    Artist = "SomeArtist",
                    AuthorString = "SomeAuthor",
                    Title = "import"
                };

                beatmap = Game.BeatmapManager.Import(new BeatmapSetInfo
                {
                    Hash = Guid.NewGuid().ToString(),
                    OnlineID = 1,
                    Metadata = metadata,
                    Beatmaps =
                    {
                        new BeatmapInfo
                        {
                            OnlineID = 1 * 1024,
                            Metadata = metadata,
                            BaseDifficulty = difficulty,
                            Ruleset = new OsuRuleset().RulesetInfo
                        },
                        new BeatmapInfo
                        {
                            OnlineID = 1 * 2048,
                            Metadata = metadata,
                            BaseDifficulty = difficulty,
                            Ruleset = new OsuRuleset().RulesetInfo
                        },
                    }
                }).Result.Value;
            });
        }

        [Test]
        public void TestFromMainMenu([Values] ScorePresentType type)
        {
            var firstImport = importScore(1);
            var secondimport = importScore(3);

            presentAndConfirm(firstImport, type);
            returnToMenu();
            presentAndConfirm(secondimport, type);
            returnToMenu();
            returnToMenu();
        }

        [Test]
        public void TestFromMainMenuDifferentRuleset([Values] ScorePresentType type)
        {
            var firstImport = importScore(1);
            var secondimport = importScore(3, new ManiaRuleset().RulesetInfo);

            presentAndConfirm(firstImport, type);
            returnToMenu();
            presentAndConfirm(secondimport, type);
            returnToMenu();
            returnToMenu();
        }

        [Test]
        public void TestFromSongSelect([Values] ScorePresentType type)
        {
            var firstImport = importScore(1);
            presentAndConfirm(firstImport, type);

            var secondimport = importScore(3);
            presentAndConfirm(secondimport, type);
        }

        [Test]
        public void TestFromSongSelectDifferentRuleset([Values] ScorePresentType type)
        {
            var firstImport = importScore(1);
            presentAndConfirm(firstImport, type);

            var secondimport = importScore(3, new ManiaRuleset().RulesetInfo);
            presentAndConfirm(secondimport, type);
        }

        private void returnToMenu()
        {
            // if we don't pause, there's a chance the track may change at the main menu out of our control (due to reaching the end of the track).
            AddStep("pause audio", () =>
            {
                if (Game.MusicController.IsPlaying)
                    Game.MusicController.TogglePause();
            });

            AddStep("return to menu", () => Game.ScreenStack.CurrentScreen.Exit());
            AddUntilStep("wait for menu", () => Game.ScreenStack.CurrentScreen is MainMenu);
        }

        private Func<ScoreInfo> importScore(int i, RulesetInfo ruleset = null)
        {
            ScoreInfo imported = null;
            AddStep($"import score {i}", () =>
            {
                imported = Game.ScoreManager.Import(new ScoreInfo
                {
                    Hash = Guid.NewGuid().ToString(),
                    OnlineScoreID = i,
                    BeatmapInfo = beatmap.Beatmaps.First(),
                    Ruleset = ruleset ?? new OsuRuleset().RulesetInfo
                }).Result.Value;
            });

            AddAssert($"import {i} succeeded", () => imported != null);

            return () => imported;
        }

        /// <summary>
        /// Some tests test waiting for a particular screen twice in a row, but expect a new instance each time.
        /// There's a case where they may succeed incorrectly if we don't compare against the previous instance.
        /// </summary>
        private IScreen lastWaitedScreen;

        private void presentAndConfirm(Func<ScoreInfo> getImport, ScorePresentType type)
        {
            AddStep("present score", () => Game.PresentScore(getImport(), type));

            switch (type)
            {
                case ScorePresentType.Results:
                    AddUntilStep("wait for results", () => lastWaitedScreen != Game.ScreenStack.CurrentScreen && Game.ScreenStack.CurrentScreen is ResultsScreen);
                    AddStep("store last waited screen", () => lastWaitedScreen = Game.ScreenStack.CurrentScreen);
                    AddUntilStep("correct score displayed", () => ((ResultsScreen)Game.ScreenStack.CurrentScreen).Score.Equals(getImport()));
                    AddAssert("correct ruleset selected", () => Game.Ruleset.Value.Equals(getImport().Ruleset));
                    break;

                case ScorePresentType.Gameplay:
                    AddUntilStep("wait for player loader", () => lastWaitedScreen != Game.ScreenStack.CurrentScreen && Game.ScreenStack.CurrentScreen is ReplayPlayerLoader);
                    AddStep("store last waited screen", () => lastWaitedScreen = Game.ScreenStack.CurrentScreen);
                    AddUntilStep("correct score displayed", () => ((ReplayPlayerLoader)Game.ScreenStack.CurrentScreen).Score.Equals(getImport()));
                    AddAssert("correct ruleset selected", () => Game.Ruleset.Value.Equals(getImport().Ruleset));
                    break;
            }
        }
    }
}
