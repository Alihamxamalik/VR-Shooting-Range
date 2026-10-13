using RangeControl.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RangeControl.UI
{
    /// <summary>
    /// The big board over the lanes. It follows the round through the event channel: the countdown, then
    /// score, time, accuracy and combo while the round is live, then the results card with the medal and
    /// personal best. While a round runs it only uses TextMeshPro's numeric SetText, so it allocates nothing.
    /// </summary>
    public sealed class ScoreboardUI : MonoBehaviour
    {
        [SerializeField] RangeEvents m_Events;
        [SerializeField] GameModeController m_Controller;

        [Header("Live view")]
        [SerializeField] GameObject m_LiveRoot;
        [SerializeField] TMP_Text m_Title;
        [SerializeField] TMP_Text m_Headline;
        [SerializeField] TMP_Text m_Clock;
        [SerializeField] TMP_Text m_Accuracy;
        [SerializeField] TMP_Text m_Combo;
        [SerializeField] TMP_Text m_Best;

        [Header("Results card")]
        [SerializeField] GameObject m_ResultsRoot;
        [SerializeField] TMP_Text m_ResultTitle;
        [SerializeField] TMP_Text m_ResultScore;
        [SerializeField] TMP_Text m_ResultDetails;
        [SerializeField] TMP_Text m_ResultMedal;
        [SerializeField] Image m_MedalBadge;
        [SerializeField] TMP_Text m_ResultBest;

        [Header("Medal colours: none, bronze, silver, gold, developer")]
        [SerializeField] Color[] m_MedalColors =
        {
            new Color(0.3f, 0.3f, 0.3f), new Color(0.72f, 0.45f, 0.2f), new Color(0.78f, 0.8f, 0.83f),
            new Color(1f, 0.8f, 0.2f), new Color(0.35f, 0.85f, 1f),
        };

        static readonly string[] s_MedalNames = { "NO MEDAL", "BRONZE", "SILVER", "GOLD", "DEVELOPER" };

        int m_ShownSeconds = -1;

        void OnEnable()
        {
            m_Events.RoundStateChanged += OnRoundStateChanged;
            m_Events.ModeChanged += ShowIdle;
            m_Events.CountdownTick += OnCountdownTick;
            m_Events.ScoreAwarded += OnScoreAwarded;
            m_Events.ShotFired += OnShotFired;
            m_Events.ComboBroken += OnComboBroken;
            m_Events.RoundEnded += OnRoundEnded;
        }

        void OnDisable()
        {
            m_Events.RoundStateChanged -= OnRoundStateChanged;
            m_Events.ModeChanged -= ShowIdle;
            m_Events.CountdownTick -= OnCountdownTick;
            m_Events.ScoreAwarded -= OnScoreAwarded;
            m_Events.ShotFired -= OnShotFired;
            m_Events.ComboBroken -= OnComboBroken;
            m_Events.RoundEnded -= OnRoundEnded;
        }

        void Start()
        {
            ShowIdle();
        }

        void Update()
        {
            if (m_Controller.State != RoundState.Running)
                return;

            // The clock text is rebuilt once a second, not once a frame.
            var remaining = m_Controller.TimeRemaining;
            var seconds = remaining >= 0f ? Mathf.CeilToInt(remaining) : Mathf.FloorToInt(m_Controller.Elapsed);
            if (seconds == m_ShownSeconds)
                return;
            m_ShownSeconds = seconds;
            ShowClock(seconds);
        }

        void OnRoundStateChanged(RoundState state)
        {
            switch (state)
            {
                case RoundState.Idle:
                    ShowIdle();
                    break;
                case RoundState.Countdown:
                    ShowLive();
                    m_Clock.SetText(string.Empty);
                    m_Accuracy.SetText(string.Empty);
                    m_Combo.SetText(string.Empty);
                    break;
                case RoundState.Running:
                    ShowLive();
                    m_ShownSeconds = -1;
                    m_Headline.SetText("{0}", 0);
                    m_Accuracy.SetText("ACC -");
                    m_Combo.SetText(string.Empty);
                    break;
            }
        }

        void ShowIdle()
        {
            ShowLive();
            m_Headline.SetText("READY");
            m_Clock.SetText("Press START on the panel");
            m_Accuracy.SetText(string.Empty);
            m_Combo.SetText(string.Empty);
        }

        void ShowLive()
        {
            m_LiveRoot.SetActive(true);
            m_ResultsRoot.SetActive(false);
            var tier = m_Controller.Tier;
            m_Title.text = tier != null ? m_Controller.Mode.DisplayName.ToUpperInvariant() + " · " + tier.name.ToUpperInvariant() : m_Controller.Mode.DisplayName.ToUpperInvariant();
            m_Best.SetText("BEST {0}", m_Controller.BestScore);
        }

        void ShowClock(int seconds)
        {
            var shotsLeft = m_Controller.ShotsRemaining;
            if (shotsLeft >= 0)
            {
                m_Clock.SetText("{0} SHOTS LEFT", shotsLeft);
                return;
            }

            var minutes = seconds / 60;
            var rest = seconds % 60;
            if (rest < 10)
                m_Clock.SetText("{0}:0{1}", minutes, rest);
            else
                m_Clock.SetText("{0}:{1}", minutes, rest);
        }

        void OnCountdownTick(int secondsLeft)
        {
            m_Headline.SetText("{0}", secondsLeft);
        }

        void OnScoreAwarded(ScoreAward award)
        {
            if (!award.Counted)
                return;
            m_Headline.SetText("{0}", award.TotalScore);
            if (award.Combo > 1f)
                m_Combo.SetText("COMBO x{0:2}", award.Combo);
        }

        void OnShotFired(ShotInfo shot)
        {
            if (m_Controller.State != RoundState.Running)
                return;
            m_Accuracy.SetText("ACC {0}%", Mathf.RoundToInt(m_Controller.Score.Accuracy * 100f));
            if (m_Controller.ShotsRemaining >= 0)
                ShowClock(0);
        }

        void OnComboBroken()
        {
            m_Combo.SetText(string.Empty);
        }

        void OnRoundEnded(RoundResult result)
        {
            m_LiveRoot.SetActive(false);
            m_ResultsRoot.SetActive(true);

            m_ResultTitle.text = string.IsNullOrEmpty(result.tierName)
                ? result.modeName.ToUpperInvariant()
                : result.modeName.ToUpperInvariant() + " · " + result.tierName.ToUpperInvariant();
            m_ResultScore.text = result.score.ToString("N0");

            var details = "Accuracy " + Mathf.RoundToInt(result.accuracy * 100f) + "%   " + result.hits + " / " + result.shots + " shots";
            if (result.mode == GameMode.Precision)
            {
                details += "\nInner X " + result.innerX;
                if (result.groupSizeCm > 0f)
                    details += "   Group " + result.groupSizeCm.ToString("0.0") + " cm";
            }
            else
            {
                details += "\nBest streak " + result.bestStreak + "   Targets " + result.targetsHit + " hit, " + result.targetsMissed + " missed";
                if (result.penalties > 0)
                    details += "\nNo-shoots hit " + result.penalties;
            }

            m_ResultDetails.text = details;

            var medal = (int)result.medal;
            m_ResultMedal.gameObject.SetActive(result.hasMedals);
            m_MedalBadge.gameObject.SetActive(result.hasMedals);
            m_ResultMedal.text = s_MedalNames[medal];
            m_MedalBadge.color = m_MedalColors[Mathf.Min(medal, m_MedalColors.Length - 1)];
            m_ResultBest.text = result.isPersonalBest
                ? "NEW PERSONAL BEST"
                : "Personal best " + result.previousBest.ToString("N0");
        }
    }
}
