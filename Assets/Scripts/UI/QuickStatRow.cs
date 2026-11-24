using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 4 generic icon+number slots in the tooltip panel - which stat (and which icon) occupies each
// slot depends on what's selected:
//   plant:  Attack Damage, Attack Speed, Attack Range, Armor
//   insect: Attack Damage, Movement Speed, Armor, Magic Armor
// sibling to StatsPanelCore under StatsPanels, same selection-polling pattern as
// StatsPanelTooltip. the icon sprites are swapped here (not static) since slot 2/3/4 show a
// different stat - and so a different icon - depending on entity type
public class QuickStatRow : MonoBehaviour
{
    [SerializeField] private Image stat1Icon, stat2Icon, stat3Icon, stat4Icon;
    [SerializeField] private TMP_Text stat1Text, stat2Text, stat3Text, stat4Text;

    [Header("Icons")]
    [SerializeField] private Sprite attackDamageIcon;
    [SerializeField] private Sprite attackSpeedIcon;
    [SerializeField] private Sprite attackRangeIcon;
    [SerializeField] private Sprite armorIcon;
    [SerializeField] private Sprite movementSpeedIcon;
    [SerializeField] private Sprite magicArmorIcon;

    private void Update()
    {
        // never switches its own object off: it sits on the plant panel's StatsPanel (with the
        // hover trigger), and an object that turns itself off stops running Update, so it could
        // never come back. it used to do that the moment the selection went away while the panel
        // was still up (a selected insect dying, a selected plant sold), and the stats were gone
        // for the rest of the level. the panel hides itself when nothing is selected anyway
        PlantUpgradeUI ui = PlantUpgradeUI.instance;
        if (ui == null) return;
        // unity null checks, not ??, so a destroyed plant falls through to the insect
        Plant plant = ui.GetSelectedPlant();
        Entity entity = plant != null ? (Entity)plant : ui.GetSelectedInsect();
        if (entity == null) return;

        // the four numbers still update live, but each is only turned into text again when it
        // changes (or the row switches between a plant and an insect)
        bool isInsect = entity is Insect;
        if (isInsect != shownInsect)
        {
            shownInsect = isInsect;
            for (int i = 0; i < shown.Length; i++) shown[i] = float.NaN;
        }

        if (entity is Insect insect)
        {
            SetSlot(0, stat1Icon, stat1Text, attackDamageIcon, entity.attackDamage, "F0");
            SetSlot(1, stat2Icon, stat2Text, movementSpeedIcon, insect.movementSpeed, "F2");
            SetSlot(2, stat3Icon, stat3Text, armorIcon, entity.armor);
            SetSlot(3, stat4Icon, stat4Text, magicArmorIcon, entity.magicArmor);
        }
        else
        {
            SetSlot(0, stat1Icon, stat1Text, attackDamageIcon, entity.attackDamage, "F0");
            SetSlot(1, stat2Icon, stat2Text, attackSpeedIcon, entity.attackSpeed, "F2");
            SetSlot(2, stat3Icon, stat3Text, attackRangeIcon, entity.attackRange, "F1");
            SetSlot(3, stat4Icon, stat4Text, armorIcon, entity.armor);
        }
    }

    // the value each slot's text was last made from (NaN: not made yet)
    private readonly float[] shown = { float.NaN, float.NaN, float.NaN, float.NaN };
    private bool shownInsect;

    private void SetSlot(int slot, Image icon, TMP_Text text, Sprite sprite, float value, string format)
    {
        if (icon != null) icon.sprite = sprite;
        if (text == null || value == shown[slot]) return;
        shown[slot] = value;
        text.text = value.ToString(format);
    }

    // whole number stats, written the way int.ToString writes them
    private void SetSlot(int slot, Image icon, TMP_Text text, Sprite sprite, int value)
    {
        if (icon != null) icon.sprite = sprite;
        if (text == null || value == shown[slot]) return;
        shown[slot] = value;
        text.text = value.ToString();
    }
}
