using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns fixed-size party members and manages front/back references.
/// Character instances remain stable; only references are switched.
/// </summary>
public class PartyManager
{
    public const int PartySize = 6;
    public const int LineSize = 3;

    public readonly Fighter[] party = new Fighter[PartySize];
    public readonly Fighter[] frontLine = new Fighter[LineSize];
    public readonly Fighter[] backLine = new Fighter[LineSize];

    public PartyManager(List<Fighter> fighters)
    {
        int count = Mathf.Min(fighters.Count, PartySize);
        for (int i = 0; i < count; i++)
        {
            party[i] = fighters[i];
            if (i < LineSize) frontLine[i] = fighters[i];
            else backLine[i - LineSize] = fighters[i];
        }
    }

    public bool CanSwitch(Fighter fighter)
    {
        return fighter != null && fighter.currentHP > 0;
    }

    public bool SwapSingle(int frontIndex, int backIndex)
    {
        if (frontIndex < 0 || frontIndex >= LineSize || backIndex < 0 || backIndex >= LineSize) return false;

        Fighter front = frontLine[frontIndex];
        Fighter back = backLine[backIndex];
        if (!CanSwitch(front) || !CanSwitch(back)) return false;

        front.OnSwitchOut();
        back.OnSwitchOut();

        frontLine[frontIndex] = back;
        backLine[backIndex] = front;

        frontLine[frontIndex].OnSwitchIn();
        backLine[backIndex].OnSwitchIn();
        return true;
    }

    public bool SwapAll()
    {
        for (int i = 0; i < LineSize; i++)
        {
            if (!CanSwitch(frontLine[i]) || !CanSwitch(backLine[i])) return false;
        }

        for (int i = 0; i < LineSize; i++)
        {
            frontLine[i].OnSwitchOut();
            backLine[i].OnSwitchOut();
        }

        for (int i = 0; i < LineSize; i++)
        {
            Fighter temp = frontLine[i];
            frontLine[i] = backLine[i];
            backLine[i] = temp;
        }

        for (int i = 0; i < LineSize; i++)
        {
            frontLine[i].OnSwitchIn();
            backLine[i].OnSwitchIn();
        }

        return true;
    }
}
