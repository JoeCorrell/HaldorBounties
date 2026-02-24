using System;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HaldorBounties
{
    public static class TraderUIPatches
    {
        // ── Cached reflection ──
        private static Type _traderUIType;
        private static FieldInfo _mainPanelField;
        private static FieldInfo _buttonTemplateField;
        private static FieldInfo _tabBuyField;
        private static FieldInfo _tabSellField;
        private static FieldInfo _tabBankField;
        private static FieldInfo _activeTabField;
        private static FieldInfo _panelWidthField;
        private static FieldInfo _tabBtnHeightField;
        private static FieldInfo _leftColumnField;
        private static FieldInfo _middleColumnField;
        private static FieldInfo _rightColumnField;
        private static FieldInfo _bankContentPanelField;
        private static FieldInfo _searchFilterField;
        private static FieldInfo _searchInputField;
        private static FieldInfo _activeCategoryFilterField;
        private static FieldInfo _joyCategoryFocusIndexField;
        private static FieldInfo _colTopInsetField;
        private static FieldInfo _bottomPadField;
        private static FieldInfo _valheimFontField;
        private static FieldInfo _isVisibleField;
        private static FieldInfo _craftBtnHeightField;

        private static MethodInfo _refreshTabHighlightsMethod;
        private static MethodInfo _updateCategoryFilterVisualsMethod;
        // M-6: Cached SwitchTab — avoids GetMethod on every tab press
        private static MethodInfo _switchTabMethod;

        // ── Our state ──
        private static GameObject _tabBounties;
        private static BountyPanel _bountyPanel;
        private static bool _reflectionCached;
        private static int _preUpdateTab = -1;

        // Stored TraderUI instance for bank balance refresh
        internal static object TraderUIInstance { get; private set; }

        private static readonly Color GoldColor = new Color(0.83f, 0.64f, 0.31f, 1f);

        private static void CacheReflection()
        {
            if (_reflectionCached) return;

            _traderUIType = Type.GetType("HaldorOverhaul.TraderUI, HaldorOverhaul");
            if (_traderUIType == null)
            {
                HaldorBounties.Log.LogError("[TraderUIPatches] Could not find TraderUI type!");
                return;
            }

            var bf = BindingFlags.Instance | BindingFlags.NonPublic;
            _mainPanelField = _traderUIType.GetField("_mainPanel", bf);
            _buttonTemplateField = _traderUIType.GetField("_buttonTemplate", bf);
            _tabBuyField = _traderUIType.GetField("_tabBuy", bf);
            _tabSellField = _traderUIType.GetField("_tabSell", bf);
            _tabBankField = _traderUIType.GetField("_tabBank", bf);
            _activeTabField = _traderUIType.GetField("_activeTab", bf);
            _panelWidthField = _traderUIType.GetField("_panelWidth", bf);
            _tabBtnHeightField = _traderUIType.GetField("_tabBtnHeight", bf);
            _leftColumnField = _traderUIType.GetField("_leftColumn", bf);
            _middleColumnField = _traderUIType.GetField("_middleColumn", bf);
            _rightColumnField = _traderUIType.GetField("_rightColumn", bf);
            _bankContentPanelField = _traderUIType.GetField("_bankContentPanel", bf);
            _searchFilterField = _traderUIType.GetField("_searchFilter", bf);
            _searchInputField = _traderUIType.GetField("_searchInput", bf);
            _activeCategoryFilterField = _traderUIType.GetField("_activeCategoryFilter", bf);
            _joyCategoryFocusIndexField = _traderUIType.GetField("_joyCategoryFocusIndex", bf);
            _colTopInsetField = _traderUIType.GetField("_colTopInset", bf);
            _bottomPadField = _traderUIType.GetField("_bottomPad", bf);
            _valheimFontField = _traderUIType.GetField("_valheimFont", bf);
            _isVisibleField = _traderUIType.GetField("_isVisible", bf);
            _craftBtnHeightField = _traderUIType.GetField("_craftBtnHeight", bf);

            _refreshTabHighlightsMethod        = _traderUIType.GetMethod("RefreshTabHighlights",       bf);
            _updateCategoryFilterVisualsMethod = _traderUIType.GetMethod("UpdateCategoryFilterVisuals", bf);
            // M-6: Cache SwitchTab once instead of looking it up on every call
            _switchTabMethod = _traderUIType.GetMethod("SwitchTab", BindingFlags.Instance | BindingFlags.NonPublic);

            // C-3: Validate critical fields — log individual warnings for easier debugging
            if (_mainPanelField    == null) HaldorBounties.Log.LogWarning("[TraderUIPatches] _mainPanel field not found.");
            if (_buttonTemplateField == null) HaldorBounties.Log.LogWarning("[TraderUIPatches] _buttonTemplate field not found.");
            if (_activeTabField    == null) HaldorBounties.Log.LogWarning("[TraderUIPatches] _activeTab field not found.");
            if (_panelWidthField   == null) HaldorBounties.Log.LogWarning("[TraderUIPatches] _panelWidth field not found.");
            if (_colTopInsetField  == null) HaldorBounties.Log.LogWarning("[TraderUIPatches] _colTopInset field not found.");
            if (_bottomPadField    == null) HaldorBounties.Log.LogWarning("[TraderUIPatches] _bottomPad field not found.");
            if (_valheimFontField  == null) HaldorBounties.Log.LogWarning("[TraderUIPatches] _valheimFont field not found.");
            if (_switchTabMethod   == null) HaldorBounties.Log.LogWarning("[TraderUIPatches] SwitchTab method not found.");

            _reflectionCached = true;
            HaldorBounties.Log.LogInfo("[TraderUIPatches] Reflection cached successfully.");
        }

        // ══════════════════════════════════════════
        //  PATCH A: After BuildUI — inject 4th tab + bounty panel
        // ══════════════════════════════════════════

        [HarmonyPatch]
        private static class BuildUI_Patch
        {
            static MethodBase TargetMethod()
            {
                var t = Type.GetType("HaldorOverhaul.TraderUI, HaldorOverhaul");
                return t?.GetMethod("BuildUI", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            static void Postfix(object __instance)
            {
                try
                {
                    CacheReflection();
                    InjectBountyTab(__instance);
                }
                catch (Exception ex)
                {
                    HaldorBounties.Log.LogError($"[TraderUIPatches] BuildUI postfix error: {ex}");
                }
            }
        }

        private static void InjectBountyTab(object traderUI)
        {
            // C-3: Guard against null fields — any missing field causes NullReferenceException on GetValue
            if (_mainPanelField    == null || _buttonTemplateField == null || _activeTabField  == null ||
                _panelWidthField   == null || _tabBtnHeightField   == null || _colTopInsetField == null ||
                _bottomPadField    == null || _valheimFontField    == null)
            {
                HaldorBounties.Log.LogError("[TraderUIPatches] Cannot inject bounty tab — critical reflection fields are missing.");
                return;
            }

            var mainPanel      = (GameObject)_mainPanelField.GetValue(traderUI);
            var buttonTemplate = (GameObject)_buttonTemplateField.GetValue(traderUI);
            var tabBuy         = _tabBuyField?.GetValue(traderUI)  as GameObject;
            var tabSell        = _tabSellField?.GetValue(traderUI) as GameObject;
            var tabBank        = _tabBankField?.GetValue(traderUI) as GameObject;
            float panelWidth   = (float)_panelWidthField.GetValue(traderUI);
            float tabBtnHeight = (float)_tabBtnHeightField.GetValue(traderUI);
            float colTopInset  = (float)_colTopInsetField.GetValue(traderUI);
            float bottomPad    = (float)_bottomPadField.GetValue(traderUI);
            var font           = _valheimFontField.GetValue(traderUI) as TMP_FontAsset;

            if (mainPanel == null || buttonTemplate == null) return;

            // Calculate new tab widths — 4 equal tabs
            const float outerPad = 6f;
            const float colGap = 4f;
            const float tabTopGap = 6f;
            float usable = panelWidth - outerPad * 2f - colGap * 3f;
            float tabW = usable / 4f;

            // Reposition existing 3 tabs
            float[] centers = new float[4];
            for (int i = 0; i < 4; i++)
                centers[i] = outerPad + tabW / 2f + i * (tabW + colGap);

            ResizeTab(tabBuy, centers[0], tabW);
            ResizeTab(tabSell, centers[1], tabW);
            ResizeTab(tabBank, centers[2], tabW);

            // Create 4th tab button
            _tabBounties = UnityEngine.Object.Instantiate(buttonTemplate, mainPanel.transform);
            _tabBounties.name = "Tab_Bounties";
            _tabBounties.SetActive(true);
            var btn = _tabBounties.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SwitchToBounties(traderUI));
                btn.navigation = new Navigation { mode = Navigation.Mode.None };
            }
            var txt = _tabBounties.GetComponentInChildren<TMP_Text>(true);
            if (txt != null)
            {
                txt.text = "Bounties";
                txt.gameObject.SetActive(true);
            }

            // Strip hint children from button
            for (int i = _tabBounties.transform.childCount - 1; i >= 0; i--)
            {
                var child = _tabBounties.transform.GetChild(i);
                if (txt != null && (child.gameObject == txt.gameObject || txt.transform.IsChildOf(child)))
                    continue;
                UnityEngine.Object.Destroy(child.gameObject);
            }

            var tabRT = _tabBounties.GetComponent<RectTransform>();
            tabRT.anchorMin = new Vector2(0f, 1f);
            tabRT.anchorMax = new Vector2(0f, 1f);
            tabRT.pivot = new Vector2(0.5f, 1f);
            tabRT.sizeDelta = new Vector2(tabW, tabBtnHeight);
            tabRT.anchoredPosition = new Vector2(centers[3], -tabTopGap);

            // Get button height from TraderUI
            float craftBtnHeight = 30f;
            if (_craftBtnHeightField != null)
                craftBtnHeight = (float)_craftBtnHeightField.GetValue(traderUI);

            // Store TraderUI instance for bank balance refresh from BountyManager
            TraderUIInstance = traderUI;

            // Build bounty panel (sprite loaded from embedded resource inside BountyPanel)
            _bountyPanel = new BountyPanel();
            _bountyPanel.Build(mainPanel.transform, colTopInset, bottomPad, font, buttonTemplate, craftBtnHeight);

            HaldorBounties.Log.LogInfo("[TraderUIPatches] Bounty tab injected.");
        }

        private static void ResizeTab(GameObject tab, float centerX, float width)
        {
            if (tab == null) return;
            var rt = tab.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
            rt.anchoredPosition = new Vector2(centerX, rt.anchoredPosition.y);
        }

        // ══════════════════════════════════════════
        //  PATCH B: SwitchTab — handle tab 3
        // ══════════════════════════════════════════

        [HarmonyPatch]
        private static class SwitchTab_Patch
        {
            static MethodBase TargetMethod()
            {
                var t = Type.GetType("HaldorOverhaul.TraderUI, HaldorOverhaul");
                return t?.GetMethod("SwitchTab", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            static bool Prefix(object __instance, int newTab)
            {
                CacheReflection();
                // C-3: Guard against missing field
                if (_activeTabField == null) return true;

                if (newTab == 3)
                {
                    // Handle bounty tab ourselves
                    int currentTab = (int)_activeTabField.GetValue(__instance);
                    if (currentTab == 3) return false; // already on bounties

                    _activeTabField.SetValue(__instance, 3);

                    // Clear search/filters
                    _searchFilterField?.SetValue(__instance, "");
                    var searchInput = _searchInputField?.GetValue(__instance) as TMP_InputField;
                    if (searchInput != null) searchInput.text = "";
                    _activeCategoryFilterField?.SetValue(__instance, null);
                    _joyCategoryFocusIndexField?.SetValue(__instance, -1);
                    _updateCategoryFilterVisualsMethod?.Invoke(__instance, null);

                    // Update tab highlights
                    RefreshAllTabHighlights(__instance);

                    // Hide columns, show bounty panel
                    HideColumns(__instance);
                    if (_bankContentPanelField?.GetValue(__instance) is GameObject bankPanel)
                        bankPanel.SetActive(false);

                    if (_bountyPanel?.Root != null)
                    {
                        _bountyPanel.Root.SetActive(true);
                        _bountyPanel.Refresh();
                    }

                    // Clear event system selection
                    if (UnityEngine.EventSystems.EventSystem.current != null)
                        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

                    return false; // skip original
                }

                // Not tab 3 — let original run, but hide bounty panel
                if (_bountyPanel?.Root != null)
                    _bountyPanel.Root.SetActive(false);

                return true; // run original
            }
        }

        // ══════════════════════════════════════════
        //  PATCH C: RefreshTabHighlights — include bounty tab
        // ══════════════════════════════════════════

        [HarmonyPatch]
        private static class RefreshTabHighlights_Patch
        {
            static MethodBase TargetMethod()
            {
                var t = Type.GetType("HaldorOverhaul.TraderUI, HaldorOverhaul");
                return t?.GetMethod("RefreshTabHighlights", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            static void Postfix(object __instance)
            {
                CacheReflection();
                if (_tabBounties == null || _activeTabField == null) return;

                int activeTab = (int)_activeTabField.GetValue(__instance);
                var btn = _tabBounties.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = true;
                    btn.transition = Selectable.Transition.None;
                }
                var img = _tabBounties.GetComponent<Image>();
                if (img != null)
                    img.color = (activeTab == 3) ? GoldColor : new Color(0.45f, 0.45f, 0.45f, 1f);
            }
        }

        // ══════════════════════════════════════════
        //  PATCH D: RefreshTabPanels — hide bounty panel
        // ══════════════════════════════════════════

        [HarmonyPatch]
        private static class RefreshTabPanels_Patch
        {
            static MethodBase TargetMethod()
            {
                var t = Type.GetType("HaldorOverhaul.TraderUI, HaldorOverhaul");
                return t?.GetMethod("RefreshTabPanels", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            static void Postfix(object __instance)
            {
                // Original only handles tabs 0-2, so bounty panel stays visible if we don't hide it
                if (_bountyPanel?.Root != null)
                    _bountyPanel.Root.SetActive(false);
            }
        }

        // ══════════════════════════════════════════
        //  PATCH E: Update — extend Q/E and gamepad range to 3
        // ══════════════════════════════════════════

        [HarmonyPatch]
        private static class Update_Patch
        {
            static MethodBase TargetMethod()
            {
                var t = Type.GetType("HaldorOverhaul.TraderUI, HaldorOverhaul");
                return t?.GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            // Capture tab BEFORE TraderUI.Update processes input, so we know
            // the real starting tab and don't double-fire on the same button press.
            static void Prefix(object __instance)
            {
                CacheReflection();
                if (_activeTabField == null) { _preUpdateTab = -1; return; }
                _preUpdateTab = (int)_activeTabField.GetValue(__instance);
            }

            static void Postfix(object __instance)
            {
                if (_isVisibleField == null || _activeTabField == null) return;
                bool isVisible = (bool)_isVisibleField.GetValue(__instance);
                if (!isVisible) return;

                int activeTab = (int)_activeTabField.GetValue(__instance);

                var searchInput   = _searchInputField?.GetValue(__instance) as TMP_InputField;
                bool searchFocused = searchInput != null && searchInput.isFocused;
                if (searchFocused) return;

                // H-6: Use a local flag to ensure SwitchToBounties is called at most once per Update frame.
                bool switchHandled = false;

                // ── Tab 3 protection ──
                if (_preUpdateTab == 3 && activeTab != 3)
                {
                    bool leftPressed = Input.GetKeyDown(KeyCode.Q) || ZInput.GetButtonDown("JoyTabLeft");
                    if (!leftPressed)
                    {
                        // Undo TraderUI's unwanted tab change (e.g. JoyTabRight on rightmost tab)
                        SwitchToBounties(__instance);
                        switchHandled = true;
                    }
                }
                // ── Extend right from tab 2 into tab 3 ──
                else if (!switchHandled && _preUpdateTab == 2 && activeTab == 2)
                {
                    if (Input.GetKeyDown(KeyCode.E) || ZInput.GetButtonDown("JoyTabRight"))
                    {
                        SwitchToBounties(__instance);
                        switchHandled = true;
                    }
                }

                // Per-frame bounty panel update (live timer + gamepad input)
                activeTab = (int)_activeTabField.GetValue(__instance);
                if (activeTab == 3)
                    _bountyPanel?.UpdatePerFrame();
            }
        }

        // ══════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════

        // M-6: Use cached MethodInfo — no per-call reflection overhead
        private static readonly object[] _tab3Args    = new object[] { 3 };
        private static readonly object[] _tab2Args    = new object[] { 2 };

        private static void SwitchToBounties(object traderUI)
        {
            _switchTabMethod?.Invoke(traderUI, _tab3Args);
        }

        private static void SwitchFromBounties(object traderUI, int targetTab)
        {
            if (_bountyPanel?.Root != null) _bountyPanel.Root.SetActive(false);
            // Set to bounties tab first so SwitchTab's early-exit check doesn't skip
            _activeTabField?.SetValue(traderUI, 3);
            _switchTabMethod?.Invoke(traderUI, new object[] { targetTab });
        }

        private static void RefreshAllTabHighlights(object traderUI)
        {
            // Call the original, which handles tabs 0-2
            _refreshTabHighlightsMethod?.Invoke(traderUI, null);
            // Our postfix handles the bounties tab
        }

        private static void HideColumns(object traderUI)
        {
            var left = _leftColumnField?.GetValue(traderUI) as RectTransform;
            var mid = _middleColumnField?.GetValue(traderUI) as RectTransform;
            var right = _rightColumnField?.GetValue(traderUI) as RectTransform;

            if (left != null) left.gameObject.SetActive(false);
            if (mid != null) mid.gameObject.SetActive(false);
            if (right != null) right.gameObject.SetActive(false);
        }
    }
}
