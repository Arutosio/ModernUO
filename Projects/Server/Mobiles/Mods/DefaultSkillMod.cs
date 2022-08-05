/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DefaultSkillMod.cs                                              *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using ModernUO.Serialization;

namespace Server;

[SerializationGenerator(0)]
public partial class DefaultSkillMod : SkillMod
{
    public DefaultSkillMod(Mobile owner) : base(owner)
    {
    }

    public DefaultSkillMod(SkillName skill, bool relative, double value) : base(skill, relative, value)
    {
    }

    public override bool CheckCondition() => true;
}
