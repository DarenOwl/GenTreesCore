﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenTreesCore.Entities
{
    public abstract class Relation
    {
        /// <summary>
        /// Человек, к которому применяется данное родство
        /// </summary>
        public Person TargetPerson { get; set; }
    }

    public class SpouseRelation : Relation
    {
        /// <summary>
        /// Был ли брак расторгнут по тем или иным причинам
        /// </summary>
        public bool IsFinished { get; set; }
    }

    public class ChildRelation : Relation
    {
        /// <summary>
        /// Второй родитель
        /// </summary>
        public Person SecondParent { get; set; }
        /// <summary>
        /// Степень родства
        /// </summary>
        public RealtionRate RealtionRate { get; set; }
    }

    public enum RealtionRate
    {
        /// <summary>
        /// Кровный родственник
        /// </summary>
        BloodRelative,
        /// <summary>
        /// Не кровный родственник
        /// </summary>
        NotBloodRelative
    }
}
