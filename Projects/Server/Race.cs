using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Server;

public abstract class Race : ISpanParsable<Race>
{
    public enum EnumRaces { Human = 0, Elf = 1, Gargoyle = 2, DarkElf = 3, Orc = 4, Vampire = 5, Dwarf = 6, Demon = 7 }
    protected Race(
        int raceID, int raceIndex, string name, string pluralName, int maleBody, int femaleBody,
        int maleGhostBody, int femaleGhostBody, Expansion requiredExpansion
    )
    {
        RaceID = raceID;
        RaceIndex = raceIndex;
        RaceFlag = 1 << raceIndex;

        Name = name;

        MaleBody = maleBody;
        FemaleBody = femaleBody;
        MaleGhostBody = maleGhostBody;
        FemaleGhostBody = femaleGhostBody;

        RequiredExpansion = requiredExpansion;
        PluralName = pluralName;
    }

    public static Race[] Races { get; } = new Race[0x100];

    public static Race DefaultRace => Races[(int)EnumRaces.Human];

    public static Race Human => Races[(int)EnumRaces.Human];
    public static Race Elf => Races[(int)EnumRaces.Elf];
    public static Race Gargoyle => Races[(int)EnumRaces.Gargoyle];

    //Custom Races
    public static Race DarkElf => Races[(int)EnumRaces.DarkElf];
    public static Race Orc => Races[(int)EnumRaces.Orc];
    public static Race Vampire => Races[(int)EnumRaces.Vampire];
    public static Race Dwarf => Races[(int)EnumRaces.Dwarf];
    public static Race Demon => Races[(int)EnumRaces.Demon];

    public static List<Race> AllRaces { get; } = new();

    public const int AllowAllRaces = 0x7;      // Race.Human.RaceFlag | Race.Elf.RaceFlag | Race.Gargoyle.RaceFlag
    public const int AllowHumanOrElves = 0x3;  // Race.Human.RaceFlag | Race.Elf.RaceFlag
    public const int AllowElvesOnly = 0x2;     // Race.Elf.RaceFlag
    public const int AllowGargoylesOnly = 0x4; // Race.Gargoyle.RaceFlag

    public static Race GetRace(string value)
    {
        foreach (Race r in Races)
            if (r.Name.ToLower().Equals(value.ToLower()))
                return r;
        return null;
    }

    public Expansion RequiredExpansion { get; }

    public int MaleBody { get; }

    public int MaleGhostBody { get; }

    public int FemaleBody { get; }

    public int FemaleGhostBody { get; }

    public int RaceID { get; }

    public int RaceIndex { get; }

    public int RaceFlag { get; }

    public string Name { get; set; }

    public string PluralName { get; set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAllowedRace(Race race, int allowedRaceFlags) => (allowedRaceFlags & race.RaceFlag) != 0;

    public override string ToString() => Name;

    public virtual bool ValidateHair(Mobile m, int itemID) => ValidateHair(m.Female, itemID);

    public abstract bool ValidateHair(bool female, int itemID);

    public virtual int RandomHair(Mobile m) => RandomHair(m.Female);

    public abstract int RandomHair(bool female);

    public virtual bool ValidateFacialHair(Mobile m, int itemID) => ValidateFacialHair(m.Female, itemID);

    public abstract bool ValidateFacialHair(bool female, int itemID);

    public virtual int RandomFacialHair(Mobile m) => RandomFacialHair(m.Female);

    public abstract int RandomFacialHair(bool female); // For the *ahem* bearded ladies

    public abstract int ClipSkinHue(int hue);
    public abstract int RandomSkinHue();

    public abstract int ClipHairHue(int hue);
    public abstract int RandomHairHue();

    public virtual int Body(Mobile m) => m.Alive ? AliveBody(m.Female) : GhostBody(m.Female);

    public virtual int AliveBody(Mobile m) => AliveBody(m.Female);

    public virtual int AliveBody(bool female) => female ? FemaleBody : MaleBody;

    public virtual int GhostBody(Mobile m) => GhostBody(m.Female);

    public virtual int GhostBody(bool female) => female ? FemaleGhostBody : MaleGhostBody;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Race Parse(string s) => Parse(s, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Race Parse(string s, IFormatProvider provider) => Parse(s.AsSpan(), provider);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(string s, IFormatProvider provider, out Race result) =>
        TryParse(s.AsSpan(), provider, out result);

    public static Race Parse(ReadOnlySpan<char> s, IFormatProvider provider)
    {
        if (int.TryParse(s, out var index) && index >= 0 && index < Races.Length)
        {
            var race = Races[index];
            if (race != null)
            {
                return race;
            }
        }

        s = s.Trim();

        for (var i = 0; i < Races.Length; ++i)
        {
            var race = Races[i];
            if (s.InsensitiveEquals(race.Name) || s.InsensitiveEquals(race.PluralName))
            {
                return race;
            }
        }

        throw new FormatException($"The input string '{s}' was not in a correct format.");
    }

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out Race result)
    {
        if (int.TryParse(s, out var index) && index >= 0 && index < Races.Length)
        {
            result = Races[index];
            return result != null;
        }

        s = s.Trim();

        for (var i = 0; i < Races.Length; ++i)
        {
            var race = Races[i];
            if (s.InsensitiveEquals(race.Name) || s.InsensitiveEquals(race.PluralName))
            {
                result = race;
                return true;
            }
        }

        result = default;
        return false;
    }
}
