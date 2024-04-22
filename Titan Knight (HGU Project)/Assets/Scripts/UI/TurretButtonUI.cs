using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurretButtonUI : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TextMeshProUGUI _nameText;

    [SerializeField] private TextMeshProUGUI _costText;
    [SerializeField] private Image _turretIcon;

    [Tooltip("Internal tracker for the index of the turret within the list of turrets in the loadout; currently used for UI pruposes")] private static int loadoutIndex = 0;
    public int index;
    
    void Awake()
    {
        index = loadoutIndex;
        loadoutIndex++;
    }

    public void SetBaseTurret(Turret turret, bool assignBuildMenuFunctionailty)
    {
        //Assigns this button to the type of turret it is for
        _nameText.text = turret.turretName;
         if (_costText != null) _costText.text = turret.cost.ToString();
        _turretIcon.sprite = turret.menuIcon;

        if (assignBuildMenuFunctionailty)
        {
            // Assign this button the proper onClick functionality
            _button.onClick.AddListener(() => {
                BuildMenu.Instance.BuildButtonPressed(turret);
            });
        }

    }
}
