using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HaldorBounties
{
    public class BountyPanel
    {
        // Colors matching TraderUI
        private static readonly Color GoldColor = new Color(0.83f, 0.64f, 0.31f, 1f);
        private static readonly Color GoldTextColor = new Color(0.93f, 0.80f, 0.45f, 1f);
        private static readonly Color GrayColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        private static readonly Color DimColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        private static readonly Color ActiveColor = new Color(0.9f, 0.78f, 0.3f, 1f);
        private static readonly Color ReadyColor = new Color(0.3f, 0.9f, 0.3f, 1f);
        private static readonly Color ClaimedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        private static readonly Color AbandonColor = new Color(0.8f, 0.3f, 0.3f, 1f);

        private GameObject _root;
        private TMP_FontAsset _font;
        private GameObject _buttonTemplate;
        private float _buttonHeight = 36f;

        // Left side — bounty list (split into Available / Active sections)
        private RectTransform _availableContent;
        private RectTransform _activeContent;
        private ScrollRect _availableScroll;
        private ScrollRect _activeScroll;
        private readonly List<BountyListRow> _rows = new List<BountyListRow>();
        private TMP_Text _dayLabel;

        // Right side — detail
        private TMP_Text _detailTitle;
        private TMP_Text _detailDesc;
        private TMP_Text _detailProgress;
        private TMP_Text _detailReward;
        private TMP_Text _detailTier;
        private TMP_Text _detailGoal;
        private Button _actionButton;
        private TMP_Text _actionButtonLabel;
        private Image _progressBarFill;
        private GameObject _progressBarRoot;

        private string _selectedBountyId;
        private int _lastDisplayedDay = -1;

        // Reward choice UI
        private GameObject _rewardChoiceRow;
        private readonly List<Button> _rewardChoiceButtons = new List<Button>();
        private List<RewardResolver.ResolvedReward> _currentRewards;
        private Sprite _catBtnSprite;
        private Sprite _progressBarSprite;

        // Detail scroll
        private ScrollRect _detailDescScroll;

        // Gamepad navigation
        private int _focusedRewardIndex = -1;

        private class BountyListRow
        {
            public string BountyId;
            public string Tier;
            public string Type;
            public GameObject GO;
            public TMP_Text Title;
            public Image Background;
        }

        public GameObject Root => _root;

        public void Build(Transform parent, float colTopInset, float bottomPad, TMP_FontAsset font, GameObject buttonTemplate, float buttonHeight)
        {
            _font = font;
            _buttonTemplate = buttonTemplate;
            _buttonHeight = Mathf.Max(buttonHeight, 30f);
            LoadCatBtnSprite();
            LoadProgressBarSprite();

            // Full-width panel (same as bank panel)
            _root = new GameObject("BountyContent", typeof(RectTransform), typeof(Image));
            _root.transform.SetParent(parent, false);
            var rt = _root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(6f, bottomPad);
            rt.offsetMax = new Vector2(-6f, -colTopInset);
            _root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            // ── Left panel (bounty list, split into Available / Active) ──
            var leftPanel = new GameObject("LeftPanel", typeof(RectTransform));
            leftPanel.transform.SetParent(_root.transform, false);
            var leftRT = leftPanel.GetComponent<RectTransform>();
            leftRT.anchorMin = new Vector2(0f, 0f);
            leftRT.anchorMax = new Vector2(0.48f, 1f);
            leftRT.offsetMin = new Vector2(8f, 8f);
            leftRT.offsetMax = new Vector2(-4f, -8f);

            float sectionHeaderH = 28f;

            // ── Available section header (top of left panel) ──
            var availHdr = CreateText(leftPanel.transform, "AvailableHeader", "Available", 16f, GoldTextColor);
            availHdr.alignment = TextAlignmentOptions.MidlineLeft;
            var ahRT = availHdr.GetComponent<RectTransform>();
            ahRT.anchorMin = new Vector2(0f, 1f);
            ahRT.anchorMax = new Vector2(1f, 1f);
            ahRT.pivot = new Vector2(0.5f, 1f);
            ahRT.sizeDelta = new Vector2(-8f, sectionHeaderH);
            ahRT.anchoredPosition = new Vector2(4f, 0f);

            // Day label (right side of Available header)
            _dayLabel = CreateText(leftPanel.transform, "DayLabel", "", 12f, GrayColor);
            _dayLabel.alignment = TextAlignmentOptions.MidlineRight;
            var dayRT = _dayLabel.GetComponent<RectTransform>();
            dayRT.anchorMin = new Vector2(0f, 1f);
            dayRT.anchorMax = new Vector2(1f, 1f);
            dayRT.pivot = new Vector2(1f, 1f);
            dayRT.sizeDelta = new Vector2(-8f, sectionHeaderH);
            dayRT.anchoredPosition = new Vector2(-4f, 0f);

            // ── Available scroll (top half, below header to midpoint) ──
            _availableContent = BuildScrollSection(leftPanel.transform,
                new Vector2(0f, 0.5f), new Vector2(1f, 1f),
                new Vector2(0f, 2f), new Vector2(0f, -sectionHeaderH));
            _availableScroll = _availableContent.transform.parent.parent.GetComponent<ScrollRect>();

            // ── Section separator at midpoint ──
            var secSep = new GameObject("SectionSep", typeof(RectTransform), typeof(Image));
            secSep.transform.SetParent(leftPanel.transform, false);
            var ssRT = secSep.GetComponent<RectTransform>();
            ssRT.anchorMin = new Vector2(0f, 0.5f);
            ssRT.anchorMax = new Vector2(1f, 0.5f);
            ssRT.pivot = new Vector2(0.5f, 0.5f);
            ssRT.sizeDelta = new Vector2(-8f, 1f);
            ssRT.anchoredPosition = Vector2.zero;
            secSep.GetComponent<Image>().color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.3f);

            // ── Active section header (top of bottom half) ──
            var activeHdr = CreateText(leftPanel.transform, "ActiveHeader", "Active", 16f, ActiveColor);
            activeHdr.alignment = TextAlignmentOptions.MidlineLeft;
            var actRT = activeHdr.GetComponent<RectTransform>();
            actRT.anchorMin = new Vector2(0f, 0.5f);
            actRT.anchorMax = new Vector2(1f, 0.5f);
            actRT.pivot = new Vector2(0.5f, 1f);
            actRT.sizeDelta = new Vector2(-8f, sectionHeaderH);
            actRT.anchoredPosition = new Vector2(4f, -2f);

            // ── Active scroll (bottom half, below active header to bottom) ──
            _activeContent = BuildScrollSection(leftPanel.transform,
                new Vector2(0f, 0f), new Vector2(1f, 0.5f),
                new Vector2(0f, 0f), new Vector2(0f, -(sectionHeaderH + 4f)));
            _activeScroll = _activeContent.transform.parent.parent.GetComponent<ScrollRect>();

            // ── Vertical separator ──
            var vsep = new GameObject("VSeparator", typeof(RectTransform), typeof(Image));
            vsep.transform.SetParent(_root.transform, false);
            var vsRT = vsep.GetComponent<RectTransform>();
            vsRT.anchorMin = new Vector2(0.48f, 0f);
            vsRT.anchorMax = new Vector2(0.48f, 1f);
            vsRT.pivot = new Vector2(0.5f, 0.5f);
            vsRT.sizeDelta = new Vector2(1f, 0f);
            vsRT.offsetMin = new Vector2(-0.5f, 12f);
            vsRT.offsetMax = new Vector2(0.5f, -8f);
            vsep.GetComponent<Image>().color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.3f);

            // ── Right panel (detail) ──
            var rightPanel = new GameObject("RightPanel", typeof(RectTransform));
            rightPanel.transform.SetParent(_root.transform, false);
            var rpRT = rightPanel.GetComponent<RectTransform>();
            rpRT.anchorMin = new Vector2(0.48f, 0f);
            rpRT.anchorMax = new Vector2(1f, 1f);
            rpRT.offsetMin = new Vector2(4f, 8f);
            rpRT.offsetMax = new Vector2(-8f, -8f);

            BuildDetailPanel(rightPanel.transform);

            _root.SetActive(false);
        }

        private RectTransform BuildScrollSection(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(parent, false);
            var scrollRT = scrollGO.GetComponent<RectTransform>();
            scrollRT.anchorMin = anchorMin;
            scrollRT.anchorMax = anchorMax;
            scrollRT.offsetMin = offsetMin;
            scrollRT.offsetMax = offsetMax;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGO.transform, false);
            var vpRT = viewport.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero;
            vpRT.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.sizeDelta = new Vector2(0f, 0f);

            var scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.content = contentRT;
            scrollRect.viewport = vpRT;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 30f;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            return contentRT;
        }

        private void BuildDetailPanel(Transform parent)
        {
            float yPos = -8f;

            // Title
            _detailTitle = CreateText(parent, "DetailTitle", "", 22f, GoldTextColor);
            var dtRT = _detailTitle.GetComponent<RectTransform>();
            dtRT.anchorMin = new Vector2(0f, 1f);
            dtRT.anchorMax = new Vector2(1f, 1f);
            dtRT.pivot = new Vector2(0.5f, 1f);
            dtRT.sizeDelta = new Vector2(-16f, 30f);
            dtRT.anchoredPosition = new Vector2(0f, yPos);
            yPos -= 34f;

            // Tier badge
            _detailTier = CreateText(parent, "DetailTier", "", 12f, GrayColor);
            _detailTier.alignment = TextAlignmentOptions.MidlineLeft;
            var tierRT = _detailTier.GetComponent<RectTransform>();
            tierRT.anchorMin = new Vector2(0f, 1f);
            tierRT.anchorMax = new Vector2(1f, 1f);
            tierRT.pivot = new Vector2(0.5f, 1f);
            tierRT.sizeDelta = new Vector2(-16f, 18f);
            tierRT.anchoredPosition = new Vector2(0f, yPos);
            yPos -= 22f;

            // Description (wrapped in ScrollRect for right-stick scrolling)
            float descH = 100f;
            var descScrollGO = new GameObject("DescScroll", typeof(RectTransform), typeof(ScrollRect));
            descScrollGO.transform.SetParent(parent, false);
            var dsRT = descScrollGO.GetComponent<RectTransform>();
            dsRT.anchorMin = new Vector2(0f, 1f);
            dsRT.anchorMax = new Vector2(1f, 1f);
            dsRT.pivot = new Vector2(0.5f, 1f);
            dsRT.sizeDelta = new Vector2(-16f, descH);
            dsRT.anchoredPosition = new Vector2(0f, yPos);

            var descVP = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            descVP.transform.SetParent(descScrollGO.transform, false);
            var vpRT2 = descVP.GetComponent<RectTransform>();
            vpRT2.anchorMin = Vector2.zero;
            vpRT2.anchorMax = Vector2.one;
            vpRT2.offsetMin = Vector2.zero;
            vpRT2.offsetMax = Vector2.zero;
            descVP.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            descVP.GetComponent<Mask>().showMaskGraphic = false;

            var descContent = new GameObject("Content", typeof(RectTransform));
            descContent.transform.SetParent(descVP.transform, false);
            var dcRT = descContent.GetComponent<RectTransform>();
            dcRT.anchorMin = new Vector2(0f, 1f);
            dcRT.anchorMax = new Vector2(1f, 1f);
            dcRT.pivot = new Vector2(0.5f, 1f);
            dcRT.sizeDelta = new Vector2(0f, descH);

            _detailDescScroll = descScrollGO.GetComponent<ScrollRect>();
            _detailDescScroll.content = dcRT;
            _detailDescScroll.viewport = vpRT2;
            _detailDescScroll.horizontal = false;
            _detailDescScroll.vertical = true;
            _detailDescScroll.scrollSensitivity = 20f;
            _detailDescScroll.movementType = ScrollRect.MovementType.Clamped;

            _detailDesc = CreateText(descContent.transform, "DetailDesc", "", 15f, new Color(0.85f, 0.85f, 0.85f, 1f));
            _detailDesc.alignment = TextAlignmentOptions.TopLeft;
            _detailDesc.textWrappingMode = TextWrappingModes.Normal;
            _detailDesc.overflowMode = TextOverflowModes.Overflow;
            _detailDesc.enableAutoSizing = false;
            var ddRT = _detailDesc.GetComponent<RectTransform>();
            ddRT.anchorMin = new Vector2(0f, 1f);
            ddRT.anchorMax = new Vector2(1f, 1f);
            ddRT.pivot = new Vector2(0.5f, 1f);
            ddRT.sizeDelta = new Vector2(0f, descH);
            ddRT.anchoredPosition = Vector2.zero;
            yPos -= (descH + 8f);

            // Separator
            CreateLocalSeparator(parent, yPos);
            yPos -= 12f;

            // Goal text — anchored to top, directly below separator
            _detailGoal = CreateText(parent, "DetailGoal", "", 16f, Color.white);
            _detailGoal.alignment = TextAlignmentOptions.Center;
            _detailGoal.fontStyle = FontStyles.Bold;
            var goalRT = _detailGoal.GetComponent<RectTransform>();
            goalRT.anchorMin = new Vector2(0f, 1f);
            goalRT.anchorMax = new Vector2(1f, 1f);
            goalRT.pivot = new Vector2(0.5f, 1f);
            goalRT.sizeDelta = new Vector2(-16f, 24f);
            goalRT.anchoredPosition = new Vector2(0f, yPos);
            yPos -= 28f;

            // Reward Options label — anchored to top
            _detailReward = CreateText(parent, "DetailReward", "Reward Options:", 14f, GoldTextColor);
            _detailReward.alignment = TextAlignmentOptions.Center;
            var drRT = _detailReward.GetComponent<RectTransform>();
            drRT.anchorMin = new Vector2(0f, 1f);
            drRT.anchorMax = new Vector2(1f, 1f);
            drRT.pivot = new Vector2(0.5f, 1f);
            drRT.sizeDelta = new Vector2(-16f, 20f);
            drRT.anchoredPosition = new Vector2(0f, yPos);
            yPos -= 26f;

            // Reward choice row — 4 large square buttons, anchored to top
            try { BuildRewardChoiceRow(parent, yPos); }
            catch (Exception ex) { HaldorBounties.Log.LogWarning($"[BountyPanel] BuildRewardChoiceRow failed: {ex}"); }

            // ── Bottom section (anchored to bottom of detail panel) ──
            // Action button — anchored to bottom
            if (_buttonTemplate != null)
            {
                _actionButton = CreateActionButton(parent);
            }
            float bottomY = 8f + _buttonHeight + 4f;

            // Progress bar — directly above action button, same width as button
            _progressBarRoot = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image));
            _progressBarRoot.transform.SetParent(parent, false);
            var pbRT = _progressBarRoot.GetComponent<RectTransform>();
            pbRT.anchorMin = new Vector2(0f, 0f);
            pbRT.anchorMax = new Vector2(1f, 0f);
            pbRT.pivot = new Vector2(0.5f, 0f);
            pbRT.sizeDelta = new Vector2(-24f, 26f);
            pbRT.anchoredPosition = new Vector2(0f, bottomY);
            var pbImg = _progressBarRoot.GetComponent<Image>();
            if (_progressBarSprite != null)
            {
                pbImg.sprite = _progressBarSprite;
                pbImg.type = Image.Type.Simple;
                pbImg.color = Color.white;
            }
            else
            {
                pbImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            }

            var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(_progressBarRoot.transform, false);
            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(0f, 1f);
            fillRT.offsetMin = new Vector2(1f, 1f);
            fillRT.offsetMax = new Vector2(0f, -1f);
            fillRT.pivot = new Vector2(0f, 0.5f);
            _progressBarFill = fillGO.GetComponent<Image>();
            _progressBarFill.color = GoldColor;

            // Progress text — centered inside the progress bar, rendered on top of fill
            _detailProgress = CreateText(_progressBarRoot.transform, "DetailProgress", "", 14f, Color.white);
            _detailProgress.alignment = TextAlignmentOptions.Center;
            _detailProgress.fontStyle = FontStyles.Bold;
            var dpRT = _detailProgress.GetComponent<RectTransform>();
            dpRT.anchorMin = Vector2.zero;
            dpRT.anchorMax = Vector2.one;
            dpRT.offsetMin = Vector2.zero;
            dpRT.offsetMax = Vector2.zero;
        }

        private Button CreateActionButton(Transform parent)
        {
            var go = UnityEngine.Object.Instantiate(_buttonTemplate, parent);
            go.name = "ActionButton";
            go.SetActive(true);

            // Remove Animator — it can override button states and block clicks
            var anim = go.GetComponent("Animator") as Component;
            if (anim != null) UnityEngine.Object.DestroyImmediate(anim);

            // Remove layout components that may interfere
            foreach (var lg in go.GetComponents<LayoutGroup>()) UnityEngine.Object.DestroyImmediate(lg);
            foreach (var csf in go.GetComponents<ContentSizeFitter>()) UnityEngine.Object.DestroyImmediate(csf);

            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnActionClicked);
                btn.interactable = true;
                btn.navigation = new Navigation { mode = Navigation.Mode.None };
                btn.transition = Selectable.Transition.ColorTint;
                var colors = btn.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                colors.pressedColor = new Color(0.65f, 0.65f, 0.65f, 1f);
                colors.selectedColor = Color.white;
                colors.fadeDuration = 0.08f;
                btn.colors = colors;
            }

            _actionButtonLabel = go.GetComponentInChildren<TMP_Text>(true);
            if (_actionButtonLabel != null)
            {
                _actionButtonLabel.text = "Accept";
                _actionButtonLabel.gameObject.SetActive(true);
            }

            // Strip hint children (keep only the label text)
            var txt = go.GetComponentInChildren<TMP_Text>(true);
            for (int i = go.transform.childCount - 1; i >= 0; i--)
            {
                var child = go.transform.GetChild(i);
                if (txt != null && (child.gameObject == txt.gameObject || txt.transform.IsChildOf(child)))
                    continue;
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            // Add tint overlay (matches TraderUI buy/sell button style)
            var tintGO = new GameObject("Tint", typeof(RectTransform), typeof(Image));
            tintGO.transform.SetParent(go.transform, false);
            tintGO.transform.SetAsFirstSibling();
            var tintRT = tintGO.GetComponent<RectTransform>();
            tintRT.anchorMin = Vector2.zero;
            tintRT.anchorMax = Vector2.one;
            tintRT.offsetMin = Vector2.zero;
            tintRT.offsetMax = Vector2.zero;
            var tintImg = tintGO.GetComponent<Image>();
            tintImg.color = new Color(0f, 0f, 0f, 0.75f);
            tintImg.raycastTarget = false;

            // Bottom-anchored, full-width — matches TraderUI action button style
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(-24f, _buttonHeight);
            rt.anchoredPosition = new Vector2(0f, 8f);

            return btn;
        }

        // ── Reward choice ──

        private void LoadCatBtnSprite()
        {
            var tex = HaldorOverhaul.TextureLoader.LoadUITexture("CategoryBackground");
            if (tex != null)
            {
                _catBtnSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                // M-8: Prevent Unity from including this runtime sprite in scene serialization
                if (_catBtnSprite != null) _catBtnSprite.hideFlags = HideFlags.DontSave;
            }
        }

        private void LoadProgressBarSprite()
        {
            var tex = HaldorOverhaul.TextureLoader.LoadUITexture("SearchBarBackground");
            if (tex != null)
            {
                _progressBarSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                if (_progressBarSprite != null) _progressBarSprite.hideFlags = HideFlags.DontSave;
            }
        }

        private const float RewardBtnSize = 62f;
        private const float RewardBtnGap = 6f;

        private void BuildRewardChoiceRow(Transform parent, float yPos)
        {
            // Container — anchored to top, centered horizontally
            float totalW = 4 * RewardBtnSize + 3 * RewardBtnGap;
            _rewardChoiceRow = new GameObject("RewardChoiceRow", typeof(RectTransform));
            _rewardChoiceRow.transform.SetParent(parent, false);
            var rt = _rewardChoiceRow.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(totalW, RewardBtnSize);
            rt.anchoredPosition = new Vector2(0f, yPos);

            // 4 square buttons, manually positioned
            _rewardChoiceButtons.Clear();
            for (int i = 0; i < 4; i++)
            {
                float xPos = i * (RewardBtnSize + RewardBtnGap);
                var btn = CreateRewardChoiceButton(i, xPos);
                if (btn != null) _rewardChoiceButtons.Add(btn);
            }

            _rewardChoiceRow.SetActive(false);
        }

        private Button CreateRewardChoiceButton(int index, float xPos)
        {
            // Build from scratch — no template
            var btnGO = new GameObject($"RewardBtn_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(_rewardChoiceRow.transform, false);

            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0f, 0f);
            btnRT.anchorMax = new Vector2(0f, 0f);
            btnRT.pivot = new Vector2(0f, 0f);
            btnRT.sizeDelta = new Vector2(RewardBtnSize, RewardBtnSize);
            btnRT.anchoredPosition = new Vector2(xPos, 0f);

            // Solid opaque background
            var bgImg = btnGO.GetComponent<Image>();
            bgImg.color = new Color(0.12f, 0.12f, 0.12f, 1f);

            // Sprite overlay (CategoryBackground texture)
            if (_catBtnSprite != null)
            {
                var spriteGO = new GameObject("Sprite", typeof(RectTransform), typeof(Image));
                spriteGO.transform.SetParent(btnGO.transform, false);
                var spriteRT = spriteGO.GetComponent<RectTransform>();
                spriteRT.anchorMin = Vector2.zero;
                spriteRT.anchorMax = Vector2.one;
                spriteRT.offsetMin = Vector2.zero;
                spriteRT.offsetMax = Vector2.zero;
                var spriteImg = spriteGO.GetComponent<Image>();
                spriteImg.sprite = _catBtnSprite;
                spriteImg.type = Image.Type.Simple;
                spriteImg.color = Color.white;
                spriteImg.raycastTarget = false;
            }

            // Dark tint overlay
            var tintGO = new GameObject("Tint", typeof(RectTransform), typeof(Image));
            tintGO.transform.SetParent(btnGO.transform, false);
            var tintRT = tintGO.GetComponent<RectTransform>();
            tintRT.anchorMin = Vector2.zero;
            tintRT.anchorMax = Vector2.one;
            tintRT.offsetMin = Vector2.zero;
            tintRT.offsetMax = Vector2.zero;
            var tintImg = tintGO.GetComponent<Image>();
            tintImg.color = new Color(0f, 0f, 0f, 0.55f);
            tintImg.raycastTarget = false;

            // Button setup
            var btn = btnGO.GetComponent<Button>();
            btn.targetGraphic = bgImg;
            int idx = index;
            btn.onClick.AddListener(() => OnRewardChosen(idx));
            btn.transition = Selectable.Transition.ColorTint;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.pressedColor = new Color(0.65f, 0.65f, 0.65f, 1f);
            colors.selectedColor = Color.white;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            // Icon image (same anchors as category buttons: 0.15-0.85)
            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(btnGO.transform, false);
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.15f, 0.15f);
            iconRT.anchorMax = new Vector2(0.85f, 0.85f);
            iconRT.offsetMin = Vector2.zero;
            iconRT.offsetMax = Vector2.zero;
            var iconImg = iconGO.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.color = new Color(1f, 1f, 1f, 0.3f);

            // Quantity label — bottom-right corner
            var qtyLabel = CreateText(btnGO.transform, "Qty", "", 10f, Color.white);
            qtyLabel.alignment = TextAlignmentOptions.BottomRight;
            qtyLabel.fontStyle = FontStyles.Bold;
            var qtyRT = qtyLabel.GetComponent<RectTransform>();
            qtyRT.anchorMin = Vector2.zero;
            qtyRT.anchorMax = Vector2.one;
            qtyRT.offsetMin = new Vector2(2f, 1f);
            qtyRT.offsetMax = new Vector2(-3f, -2f);

            return btn;
        }

        private void PopulateRewardButtons()
        {
            // M-4: Always iterate the full button list so stale entries are cleared when
            // _currentRewards has fewer items than buttons (shouldn't normally happen but is safe).
            for (int i = 0; i < _rewardChoiceButtons.Count; i++)
            {
                var btn = _rewardChoiceButtons[i];
                if (btn == null) continue;

                var iconImg  = btn.transform.Find("Icon")?.GetComponent<Image>();
                var qtyLabel = btn.transform.Find("Qty")?.GetComponent<TMP_Text>();

                if (_currentRewards != null && i < _currentRewards.Count)
                {
                    var reward = _currentRewards[i];
                    if (iconImg != null)
                    {
                        var icon = RewardResolver.GetRewardIcon(reward);
                        iconImg.sprite = icon;
                        iconImg.color  = icon != null ? Color.white : new Color(1f, 1f, 1f, 0.3f);
                    }
                    if (qtyLabel != null)
                        qtyLabel.text = reward.Stack.ToString();
                }
                else
                {
                    // Clear stale data from previous bounty selection
                    if (iconImg  != null) { iconImg.sprite = null; iconImg.color = new Color(1f, 1f, 1f, 0.3f); }
                    if (qtyLabel != null) qtyLabel.text = "";
                }
            }
        }

        private void OnRewardChosen(int index)
        {
            if (string.IsNullOrEmpty(_selectedBountyId)) return;
            if (_currentRewards == null || index < 0 || index >= _currentRewards.Count) return;

            var reward = _currentRewards[index];
            BountyManager.Instance?.ClaimRewardChoice(_selectedBountyId, reward.Category);
            ForceRebuild();
        }

        // ── Per-frame update (timer + gamepad) ──

        public void UpdatePerFrame()
        {
            if (_root == null || !_root.activeSelf) return;

            // Live timer countdown
            if (_dayLabel != null && BountyManager.Instance != null)
            {
                int currentDay = BountyManager.Instance.GetCurrentDay();
                double secsLeft = BountyManager.Instance.GetSecondsUntilReset();
                int mins = (int)(secsLeft / 60);
                int secs = (int)(secsLeft % 60);
                _dayLabel.text = $"Day {currentDay}  Resets in {mins}:{secs:D2}";

                // Detect day change
                if (_lastDisplayedDay >= 0 && currentDay != _lastDisplayedDay)
                {
                    _lastDisplayedDay = currentDay;
                    ForceRebuild();
                    return;
                }
                _lastDisplayedDay = currentDay;
            }

            if (!ZInput.IsGamepadActive()) return;

            // D-pad up/down — navigate bounty list
            if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
            {
                _focusedRewardIndex = -1;
                UpdateRewardButtonVisuals();
                NavigateBountyList(1);
            }
            if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
            {
                _focusedRewardIndex = -1;
                UpdateRewardButtonVisuals();
                NavigateBountyList(-1);
            }

            // D-pad left/right — navigate reward choice buttons (only when interactable)
            bool rewardsInteractable = _rewardChoiceRow != null && _rewardChoiceRow.activeSelf
                && _rewardChoiceButtons.Count > 0 && _rewardChoiceButtons[0] != null
                && _rewardChoiceButtons[0].interactable;
            if (rewardsInteractable)
            {
                if (ZInput.GetButtonDown("JoyLStickLeft") || ZInput.GetButtonDown("JoyDPadLeft"))
                    NavigateRewardButtons(-1);
                if (ZInput.GetButtonDown("JoyLStickRight") || ZInput.GetButtonDown("JoyDPadRight"))
                    NavigateRewardButtons(1);
            }

            // A button — confirm focused reward or action button
            if (ZInput.GetButtonDown("JoyButtonA"))
            {
                if (_focusedRewardIndex >= 0 && rewardsInteractable)
                    OnRewardChosen(_focusedRewardIndex);
                else if (_actionButton != null && _actionButton.gameObject.activeSelf && _actionButton.interactable)
                    OnActionClicked();
            }

            // Right stick — scroll description
            if (_detailDescScroll != null)
            {
                float scrollSpeed = 2f;
                if (ZInput.GetButton("JoyRStickDown"))
                {
                    _detailDescScroll.verticalNormalizedPosition -= scrollSpeed * Time.deltaTime;
                    _detailDescScroll.verticalNormalizedPosition = Mathf.Clamp01(_detailDescScroll.verticalNormalizedPosition);
                }
                if (ZInput.GetButton("JoyRStickUp"))
                {
                    _detailDescScroll.verticalNormalizedPosition += scrollSpeed * Time.deltaTime;
                    _detailDescScroll.verticalNormalizedPosition = Mathf.Clamp01(_detailDescScroll.verticalNormalizedPosition);
                }
            }

            // Clear EventSystem selection to prevent Unity navigation conflicts
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        private void NavigateBountyList(int dir)
        {
            if (_rows.Count == 0) return;

            int currentIdx = -1;
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].BountyId == _selectedBountyId) { currentIdx = i; break; }
            }

            int nextIdx = currentIdx + dir;
            nextIdx = Mathf.Clamp(nextIdx, 0, _rows.Count - 1);

            if (nextIdx >= 0 && nextIdx < _rows.Count)
            {
                SelectBounty(_rows[nextIdx].BountyId);
                ScrollRowIntoView(_rows[nextIdx]);
            }
        }

        private void ScrollRowIntoView(BountyListRow row)
        {
            if (row.GO == null) return;
            var rowRT = row.GO.GetComponent<RectTransform>();

            // Determine which scroll section this row belongs to
            ScrollRect scroll = null;
            if (_availableScroll != null && rowRT.IsChildOf(_availableScroll.content))
                scroll = _availableScroll;
            else if (_activeScroll != null && rowRT.IsChildOf(_activeScroll.content))
                scroll = _activeScroll;
            if (scroll == null) return;

            float contentH = scroll.content.rect.height;
            float viewportH = scroll.viewport.rect.height;
            if (contentH <= viewportH) return;

            // Row position within the content (top-down, so y is negative)
            float rowTop = -rowRT.anchoredPosition.y;
            float rowBottom = rowTop + rowRT.sizeDelta.y;

            // Current scroll offset (0 = top, 1 = bottom in Unity's normalized coords)
            // But verticalNormalizedPosition is 1 = top, 0 = bottom
            float scrollableH = contentH - viewportH;
            float scrollTop = (1f - scroll.verticalNormalizedPosition) * scrollableH;
            float scrollBottom = scrollTop + viewportH;

            if (rowTop < scrollTop)
                scroll.verticalNormalizedPosition = 1f - (rowTop / scrollableH);
            else if (rowBottom > scrollBottom)
                scroll.verticalNormalizedPosition = 1f - ((rowBottom - viewportH) / scrollableH);
        }

        private void NavigateRewardButtons(int dir)
        {
            if (_rewardChoiceButtons.Count == 0) return;
            if (_focusedRewardIndex < 0)
                _focusedRewardIndex = dir > 0 ? 0 : _rewardChoiceButtons.Count - 1;
            else
                _focusedRewardIndex = Mathf.Clamp(_focusedRewardIndex + dir, 0, _rewardChoiceButtons.Count - 1);
            UpdateRewardButtonVisuals();
        }

        private void UpdateRewardButtonVisuals()
        {
            for (int i = 0; i < _rewardChoiceButtons.Count; i++)
            {
                var btn = _rewardChoiceButtons[i];
                if (btn == null) continue;
                var img = btn.GetComponent<Image>();
                if (img != null)
                    img.color = (i == _focusedRewardIndex)
                        ? new Color(1f, 0.75f, 0.3f, 1f)   // gold tint — focused
                        : Color.white;                        // normal
            }
        }

        // ── Refresh ──

        private bool _listBuilt;

        public void Refresh()
        {
            if (_root == null || !_root.activeSelf) return;

            // H-2: Null guard — Instance may be null if initialization failed
            if (BountyManager.Instance == null) return;

            // Check for day change + countdown
            int currentDay = BountyManager.Instance.GetCurrentDay();
            if (_dayLabel != null)
            {
                double secsLeft = BountyManager.Instance.GetSecondsUntilReset();
                int mins = (int)(secsLeft / 60);
                int secs = (int)(secsLeft % 60);
                _dayLabel.text = $"Day {currentDay}  Resets in {mins}:{secs:D2}";
            }

            if (_lastDisplayedDay >= 0 && currentDay != _lastDisplayedDay)
            {
                _listBuilt = false; // Force rebuild on day change
                _selectedBountyId = null;
            }
            _lastDisplayedDay = currentDay;

            if (!_listBuilt)
            {
                RebuildList();
                _listBuilt = true;
            }
            else
            {
                UpdateRows();
            }

            RefreshDetail();
        }

        private void RebuildList()
        {
            ClearRows();
            var bounties = BountyManager.Instance?.GetVisibleBounties() ?? new System.Collections.Generic.List<BountyEntry>();
            float rowH = 32f;
            float gap = 2f;

            // Separate into available (inactive) and active groups
            var available = new List<BountyEntry>();
            var active = new List<BountyEntry>();

            foreach (var b in bounties)
            {
                var state = BountyManager.Instance.GetState(b.Id);
                if (state == BountyState.Active || state == BountyState.Ready)
                    active.Add(b);
                else
                    available.Add(b);
            }

            // Build available rows (top section)
            float yOff = 0f;
            foreach (var b in available)
            {
                var row = CreateRow(b, yOff, _availableContent);
                _rows.Add(row);
                yOff -= (rowH + gap);
            }
            _availableContent.sizeDelta = new Vector2(0f, -yOff);

            // Build active rows (bottom section)
            yOff = 0f;
            foreach (var b in active)
            {
                var row = CreateRow(b, yOff, _activeContent);
                _rows.Add(row);
                yOff -= (rowH + gap);
            }
            _activeContent.sizeDelta = new Vector2(0f, -yOff);
        }

        public void ForceRebuild()
        {
            _listBuilt = false;
            Refresh();
        }

        private void UpdateRows()
        {
            // M-5: Detect if any row moved to a different section (e.g. Available→Active after kill completes)
            // and force a full rebuild so the list order stays correct.
            foreach (var row in _rows)
            {
                if (row.GO == null) continue;
                var state = BountyManager.Instance?.GetState(row.BountyId) ?? BountyState.Available;

                bool shouldBeActive = state == BountyState.Active || state == BountyState.Ready;
                bool isInActive     = _activeContent != null && row.GO.transform.IsChildOf(_activeContent);
                if (shouldBeActive != isInActive)
                {
                    ForceRebuild();
                    return;
                }

                bool isSelected = row.BountyId == _selectedBountyId;
                row.Background.color = isSelected
                    ? new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.3f)
                    : new Color(0.1f, 0.1f, 0.1f, 0.5f);
                row.Title.color = state == BountyState.Claimed ? DimColor : GetRowColor(row.Type, row.Tier);
            }
        }

        private BountyListRow CreateRow(BountyEntry entry, float yOffset, RectTransform parentContent)
        {
            var state = BountyManager.Instance.GetState(entry.Id);
            var row = new BountyListRow { BountyId = entry.Id, Tier = entry.Tier, Type = entry.Type };

            row.GO = new GameObject("Row_" + entry.Id, typeof(RectTransform), typeof(Image));
            row.GO.transform.SetParent(parentContent, false);
            var rt = row.GO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 32f);
            rt.anchoredPosition = new Vector2(0f, yOffset);

            row.Background = row.GO.GetComponent<Image>();
            bool isSelected = entry.Id == _selectedBountyId;
            row.Background.color = isSelected
                ? new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.3f)
                : new Color(0.1f, 0.1f, 0.1f, 0.5f);

            // Click handler
            var btn = row.GO.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            string id = entry.Id;
            btn.onClick.AddListener(() => SelectBounty(id));

            // Title (left) — colored by type/tier
            Color titleColor = state == BountyState.Claimed ? DimColor : GetRowColor(entry.Type, entry.Tier);
            row.Title = CreateText(row.GO.transform, "Title", entry.Title, 14f, titleColor);
            row.Title.alignment = TextAlignmentOptions.MidlineLeft;
            row.Title.overflowMode = TextOverflowModes.Ellipsis;
            var tRT = row.Title.GetComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0f, 0f);
            tRT.anchorMax = new Vector2(1f, 1f);
            tRT.offsetMin = new Vector2(6f, 0f);
            tRT.offsetMax = new Vector2(-6f, 0f);

            return row;
        }

        private void SelectBounty(string bountyId)
        {
            _selectedBountyId = bountyId;
            foreach (var row in _rows)
            {
                bool sel = row.BountyId == bountyId;
                row.Background.color = sel
                    ? new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.3f)
                    : new Color(0.1f, 0.1f, 0.1f, 0.5f);
            }
            RefreshDetail();
        }

        private void RefreshDetail()
        {
            if (string.IsNullOrEmpty(_selectedBountyId))
            {
                ClearDetail();
                return;
            }

            var entry = BountyConfig.Bounties.Find(b => b.Id == _selectedBountyId);
            if (entry == null) { ClearDetail(); return; }

            var state = BountyManager.Instance.GetState(_selectedBountyId);

            // For miniboss bounties, inject the boss name
            bool isMiniboss = entry.SpawnLevel > 0;
            string bossName = isMiniboss ? BountyManager.GetBossName(entry.Id) : null;

            _detailTitle.text = isMiniboss ? $"{bossName} the {entry.Title}" : entry.Title;
            // M-9: Replace only the first occurrence to avoid corrupting repeated title substrings
            _detailDesc.text = isMiniboss
                ? ReplaceFirst(entry.Description, entry.Title, $"{bossName} the {entry.Title}")
                : entry.Description;

            // Resize scroll content to fit description text
            if (_detailDescScroll != null)
            {
                _detailDesc.ForceMeshUpdate();
                float textH = _detailDesc.preferredHeight;
                _detailDesc.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, textH);
                _detailDescScroll.content.sizeDelta = new Vector2(0f, textH);
                _detailDescScroll.verticalNormalizedPosition = 1f; // scroll to top
            }

            // Tier badge
            if (_detailTier != null)
            {
                _detailTier.text = GetTierDisplay(entry.Tier);
                _detailTier.color = GetTierColor(entry.Tier);
            }

            // Progress
            int progress, target;
            if (entry.Type == "Gather" && (state == BountyState.Active || state == BountyState.Available))
            {
                progress = BountyManager.Instance.CountGatherProgress(_selectedBountyId);
                target = entry.Amount;
            }
            else
            {
                progress = BountyManager.Instance.GetProgress(_selectedBountyId);
                target = entry.Amount;
            }

            if (state == BountyState.Claimed)
            {
                _detailProgress.text = "Completed";
                _detailProgress.color = DimColor;
            }
            else if (state == BountyState.Available)
            {
                _detailProgress.text = "";
            }
            else
            {
                _detailProgress.text = $"Progress: {Mathf.Min(progress, target)} / {target}";
                _detailProgress.color = progress >= target ? ReadyColor : Color.white;
            }

            // Progress bar
            if (state == BountyState.Active || state == BountyState.Ready)
            {
                _progressBarRoot.SetActive(true);
                float pct = target > 0 ? Mathf.Clamp01((float)progress / target) : 0f;
                var fillRT = _progressBarFill.GetComponent<RectTransform>();
                fillRT.anchorMax = new Vector2(pct, 1f);
                _progressBarFill.color = pct >= 1f ? ReadyColor : GoldColor;
            }
            else
            {
                _progressBarRoot.SetActive(false);
            }

            // Rewards label + buttons
            if (_detailReward != null)
            {
                _detailReward.text = "Reward Options:";
                _detailReward.color = state == BountyState.Claimed ? DimColor : GoldTextColor;
            }

            // Always populate reward buttons with resolved rewards
            if (_rewardChoiceRow != null)
            {
                _rewardChoiceRow.SetActive(true);
                try
                {
                    _currentRewards = RewardResolver.ResolveRewards(entry);
                    PopulateRewardButtons();
                }
                catch (Exception ex)
                {
                    HaldorBounties.Log.LogWarning($"[BountyPanel] Reward resolve failed: {ex}");
                }

                // Only interactable when Ready
                bool canClaim = state == BountyState.Ready;
                if (canClaim && entry.Type == "Gather")
                {
                    int invCount = BountyManager.Instance.CountItemsInInventory(_selectedBountyId);
                    canClaim = invCount >= entry.Amount;
                }
                foreach (var btn in _rewardChoiceButtons)
                {
                    if (btn != null) btn.interactable = canClaim;
                }

                // Dim icons when claimed
                if (state == BountyState.Claimed)
                {
                    foreach (var btn in _rewardChoiceButtons)
                    {
                        if (btn == null) continue;
                        var img = btn.GetComponent<Image>();
                        if (img != null) img.color = DimColor;
                    }
                }
            }

            // Goal
            if (_detailGoal != null)
            {
                if (isMiniboss)
                {
                    string slayText = entry.Amount > 1
                        ? $"Slay {entry.Amount}x {bossName} the {entry.Title}"
                        : $"Slay {bossName} the {entry.Title}";
                    _detailGoal.text = slayText;
                }
                else
                {
                    string verb = entry.Type == "Gather" ? "Gather" : "Kill";
                    string targetName = entry.Target.Replace("_", " ");
                    _detailGoal.text = $"{verb} {entry.Amount}x {targetName}";
                }
                _detailGoal.color = state == BountyState.Claimed ? DimColor : Color.white;
            }

            // Reset reward button focus
            _focusedRewardIndex = -1;

            // Action button
            if (_actionButton != null)
            {
                switch (state)
                {
                    case BountyState.Available:
                        _actionButton.gameObject.SetActive(true);
                        _actionButton.interactable = true;
                        if (_actionButtonLabel != null) _actionButtonLabel.text = "Accept";
                        break;
                    case BountyState.Active:
                        _actionButton.gameObject.SetActive(true);
                        _actionButton.interactable = true;
                        if (_actionButtonLabel != null) _actionButtonLabel.text = "Abandon";
                        break;
                    case BountyState.Ready:
                    {
                        bool canClaim = true;
                        if (entry.Type == "Gather")
                        {
                            int invCount = BountyManager.Instance.CountItemsInInventory(_selectedBountyId);
                            canClaim = invCount >= entry.Amount;
                        }
                        if (canClaim)
                        {
                            // Reward buttons are inline — hide action button
                            _actionButton.gameObject.SetActive(false);
                            if (_detailGoal != null)
                            {
                                _detailGoal.text = "Choose your reward:";
                                _detailGoal.color = ReadyColor;
                            }
                        }
                        else
                        {
                            _actionButton.gameObject.SetActive(true);
                            _actionButton.interactable = false;
                            int invCount = BountyManager.Instance.CountItemsInInventory(_selectedBountyId);
                            if (_actionButtonLabel != null)
                                _actionButtonLabel.text = $"Need {entry.Amount - invCount} more in inventory";
                        }
                        break;
                    }
                    case BountyState.Claimed:
                        _actionButton.gameObject.SetActive(true);
                        _actionButton.interactable = false;
                        if (_actionButtonLabel != null) _actionButtonLabel.text = "Completed";
                        break;
                }
            }
        }

        private void ClearDetail()
        {
            if (_detailTitle != null) _detailTitle.text = "Select a bounty";
            if (_detailDesc != null) _detailDesc.text = "";
            if (_detailTier != null) _detailTier.text = "";
            if (_detailProgress != null) _detailProgress.text = "";
            if (_detailReward != null) _detailReward.text = "";
            if (_detailGoal != null) _detailGoal.text = "";
            if (_progressBarRoot != null) _progressBarRoot.SetActive(false);
            if (_actionButton != null) _actionButton.gameObject.SetActive(false);
            if (_rewardChoiceRow != null) _rewardChoiceRow.SetActive(false);
        }

        private void OnActionClicked()
        {
            if (string.IsNullOrEmpty(_selectedBountyId)) return;

            var state = BountyManager.Instance.GetState(_selectedBountyId);
            var entry = BountyConfig.Bounties.Find(b => b.Id == _selectedBountyId);
            if (entry == null) { ForceRebuild(); return; }

            switch (state)
            {
                case BountyState.Available:
                    BountyManager.Instance.AcceptBounty(_selectedBountyId);
                    break;
                case BountyState.Active:
                    BountyManager.Instance.AbandonBounty(_selectedBountyId);
                    break;
                // H-5: Ready state is handled exclusively by the reward choice buttons.
                // The action button is hidden when canClaim=true, so this path should
                // not fire normally. Explicitly making it a no-op prevents the gamepad
                // JoyButtonA fallback from hard-coding a coins-only claim.
                case BountyState.Ready:
                    return; // do nothing — player must click a reward button
            }
            ForceRebuild();
        }

        private void ClearRows()
        {
            foreach (var row in _rows)
                if (row.GO != null) UnityEngine.Object.Destroy(row.GO);
            _rows.Clear();
        }

        // ── Helpers ──

        // M-9: Replace only the first occurrence of a substring
        private static string ReplaceFirst(string text, string search, string replace)
        {
            int idx = text.IndexOf(search, StringComparison.Ordinal);
            return idx < 0 ? text : text.Substring(0, idx) + replace + text.Substring(idx + search.Length);
        }

        private static string GetTierDisplay(string tier)
        {
            switch (tier)
            {
                case "Easy": return "Difficulty: Easy";
                case "Medium": return "Difficulty: Medium";
                case "Hard": return "Difficulty: Hard";
                case "Miniboss": case "Special": return "Miniboss Bounty";
                default: return "";
            }
        }

        private static Color GetTierColor(string tier)
        {
            switch (tier)
            {
                case "Easy": return new Color(0.5f, 0.8f, 0.5f, 1f);
                case "Medium": return new Color(0.9f, 0.78f, 0.3f, 1f);
                case "Hard": return new Color(0.9f, 0.35f, 0.3f, 1f);
                case "Miniboss": case "Special": return new Color(0.8f, 0.3f, 0.8f, 1f);
                default: return GrayColor;
            }
        }

        private static readonly Color GatherColor = new Color(0.4f, 0.75f, 0.9f, 1f); // Light blue

        private static Color GetRowColor(string type, string tier)
        {
            if (type == "Gather") return GatherColor;
            return GetTierColor(tier);
        }

        private TMP_Text CreateText(Transform parent, string name, string text, float fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) tmp.font = _font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void CreateLocalSeparator(Transform parent, float yPos)
        {
            var sep = new GameObject("Sep", typeof(RectTransform), typeof(Image));
            sep.transform.SetParent(parent, false);
            var rt = sep.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(-16f, 1f);
            rt.anchoredPosition = new Vector2(0f, yPos);
            sep.GetComponent<Image>().color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.25f);
        }
    }
}
