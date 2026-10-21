using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HolderItem : MonoBehaviour
    {
        public UnityEngine.UI.Button EquipButton;

        public Text EquipButtonText;

        public GameObject CountLabel;

        public Text CountLabelText;

        public UnityEngine.UI.Button Button;

        public ItemShopView ItemShopView;

        public Image IconLock;

        public Text Caption;

        public Image Icon;

        public void SetCount(uint count)
        {
            CountLabel.SetActive(count != 0);
            CountLabelText.text = count.ToString();
        }

        public void Set(string caption, bool isLocked, bool isBought, bool isEquipped, int price, string icon,
            bool alwaysShowPrice = false)
        {
            // SetDefaults();
            ItemShopView.Price.gameObject.SetActive(true);

            if (isLocked)
            {
                IconLock.gameObject.SetActive(true);
            }

            Caption.text = caption;
            if (isBought)
            {
                if (alwaysShowPrice)
                {
                    ItemShopView.Header.SetActive(true);
                    ItemShopView.Price.text = price.ToString();
                }
                else
                {
                    ItemShopView.Price.gameObject.SetActive(false);
                    ItemShopView.Header.gameObject.SetActive(false);
                }

                ItemShopView.IconCheck.gameObject.SetActive(isEquipped);
                return;
            }

            ItemShopView.Header.SetActive(true);
            ItemShopView.Price.text = price.ToString();
            ItemShopView.IconPrice.gameObject.SetActive(true);
            ItemShopView.IconCheck.gameObject.SetActive(false);
        }

        void SetDefaults()
        {
            EquipButton.gameObject.SetActive(false);
            EquipButtonText.gameObject.SetActive(false);
            CountLabel.SetActive(false);
            ItemShopView.Header.SetActive(true);
            ItemShopView.IconCheck.SetActive(true);
            ItemShopView.Header.SetActive(false);
            ItemShopView.IconPrice.gameObject.SetActive(true);
            ItemShopView.Price.gameObject.SetActive(true);
            IconLock.gameObject.SetActive(false);
            Caption.gameObject.SetActive(true);
            Icon.gameObject.SetActive(true);
        }
    }
}