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
        Entity entity = (Entity)PlantUpgradeUI.instance?.GetSelectedPlant()
                     ?? PlantUpgradeUI.instance?.GetSelectedInsect();

        if (entity == null)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);

        if (entity is Insect insect)
        {
            SetSlot(stat1Icon, stat1Text, attackDamageIcon, entity.attackDamage.ToString("F0"));
            SetSlot(stat2Icon, stat2Text, movementSpeedIcon, insect.movementSpeed.ToString("F2"));
            SetSlot(stat3Icon, stat3Text, armorIcon, entity.armor.ToString());
            SetSlot(stat4Icon, stat4Text, magicArmorIcon, entity.magicArmor.ToString());
        }
        else
        {
            SetSlot(stat1Icon, stat1Text, attackDamageIcon, entity.attackDamage.ToString("F0"));
            SetSlot(stat2Icon, stat2Text, attackSpeedIcon, entity.attackSpeed.ToString("F2"));
            SetSlot(stat3Icon, stat3Text, attackRangeIcon, entity.attackRange.ToString("F1"));
            SetSlot(stat4Icon, stat4Text, armorIcon, entity.armor.ToString());
        }
    }

    private static void SetSlot(Image icon, TMP_Text text, Sprite sprite, string value)
    {
        if (icon != null) icon.sprite = sprite;
        if (text != null) text.text = value;
    }
}
