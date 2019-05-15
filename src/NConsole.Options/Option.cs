﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace NConsole.Options
{
    using static Characters;
    using static Domain;
    using static String;

    /// <summary>
    /// Represents an Option asset concern.
    /// </summary>
    public abstract class Option : IOption
    {
        /// <summary>
        /// Gets the Prototype.
        /// </summary>
        public string Prototype { get; }

        /// <summary>
        /// Gets the Description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the ValueType.
        /// </summary>
        public OptionValueType? ValueType { get; }

        /// <summary>
        /// Gets the MaximumValueCount.
        /// </summary>
        public int MaximumValueCount { get; }

        /// <summary>
        /// Gets the Names.
        /// </summary>
        internal string[] Names { get; }

        /// <summary>
        /// Gets the ValueSeparators.
        /// </summary>
        internal string[] ValueSeparators { get; private set; }

        /// <summary>
        /// <see cref="char"/> array defaults to <see cref="Equal"/> and <see cref="Colon"/>.
        /// </summary>
        private char[] NameTerminator { get; } = {Equal, Colon};

        /// <summary>
        /// Protected Constructor.
        /// </summary>
        /// <param name="prototype"></param>
        /// <param name="description"></param>
        /// <inheritdoc />
        protected Option(string prototype, string description)
            : this(prototype, description, 1)
        {
        }

        /// <summary>
        /// Protected Constructor.
        /// </summary>
        /// <param name="prototype"></param>
        /// <param name="description"></param>
        /// <param name="maximumValueCount"></param>
        protected Option(string prototype, string description, int maximumValueCount)
        {
            if (prototype == null)
            {
                throw new ArgumentNullException(nameof(prototype));
            }

            if (IsNullOrEmpty(prototype))
            {
                throw new ArgumentException("Cannot be the empty string.", nameof(prototype));
            }

            if (maximumValueCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumValueCount));
            }

            Names = prototype.Split(Pipe);
            Description = description;
            MaximumValueCount = maximumValueCount;
            ValueType = ParsePrototype(Prototype = prototype);

            ArgumentException ThrowMaximumValueCount(params OptionValueType[] traits)
            {
                var traitPrefix = $"{nameof(OptionValueType)}";

                string EnumeratedTraits() => traits.Any()
                    ? Join($" {or} ", traits.Select(x => $"`{traitPrefix}{Dot}{x}'"))
                    : "[No Traits Specified]";

                return new ArgumentException(
                    $"Cannot provide `{nameof(maximumValueCount)}' of {maximumValueCount} for {EnumeratedTraits()}."
                    , nameof(maximumValueCount));
            }

            if (MaximumValueCount == 0 && ValueType.HasValue)
            {
                throw ThrowMaximumValueCount( OptionValueType.Required, OptionValueType.Optional);
            }

            if (ValueType.HasValue && maximumValueCount > 1)
            {
                throw ThrowMaximumValueCount();
            }

            if (Array.IndexOf(Names, AngleBrackets) >= 0
                && ((Names.Length == 1 && ValueType.HasValue)
                    || (Names.Length > 1 && MaximumValueCount > 1)))
            {
                throw new ArgumentException(
                    $"The default option handler '{AngleBrackets}' cannot require values."
                    , nameof(prototype));
            }
        }

        // TODO: TBD: Seriously? This works? What are they here for? Testing? Would subscribers be using these?
        public string[] GetNames() => (string[]) Names.Clone();

        public string[] GetValueSeparators() => ValueSeparators == null ? new string[] { } : (string[]) ValueSeparators.Clone();

        /// <summary>
        /// Parses the <paramref name="value"/> given the <paramref name="context"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        protected static T Parse<T>(string value, OptionContext context)
        {
            T GetDefaultValue(out Type x)
            {
                var candidateType = typeof(T);

                bool IsNullable()
                    => candidateType.IsValueType
                       && candidateType.IsGenericType
                       && !candidateType.IsGenericTypeDefinition
                       && candidateType.GetGenericTypeDefinition() == typeof(Nullable<>);

                x = IsNullable() ? candidateType.GetGenericArguments()[0] : candidateType;
                return default;
            }

            var defaultValue = GetDefaultValue(out var targetType);
            var typeConverter = TypeDescriptor.GetConverter(targetType);

            try
            {
                if (value != null)
                {
                    defaultValue = (T) typeConverter.ConvertFromString(value);
                }
            }
            catch (Exception ex)
            {
                throw new OptionException(
                    Format(
                        context.Set.Localizer("Could not convert string `{0}' to type {1} for option `{2}'."),
                        value, targetType.Name, context.OptionName),
                    context.OptionName, ex);
            }

            return defaultValue;
        }

        /// <summary>
        /// Parses the <see cref="OptionValueType"/> given the <paramref name="prototype"/>.
        /// </summary>
        /// <param name="prototype"></param>
        /// <returns></returns>
        private OptionValueType? ParsePrototype(string prototype)
        {
            char? parsedType = null;
            var separators = new List<string>();

            OptionValueType ReturnParsedType() => parsedType == Equal ? OptionValueType.Required : OptionValueType.Optional;

            for (var i = 0; i < Names.Length; ++i)
            {
                var name = Names[i];
                if (name.Length == 0)
                {
                    throw new ArgumentException("Empty option names are not supported.", nameof(prototype));
                }

                var end = name.IndexOfAny(NameTerminator);
                if (end == -1)
                {
                    continue;
                }

                Names[i] = name.Substring(0, end);
                if (!parsedType.HasValue || parsedType == name[end])
                {
                    parsedType = name[end];
                }
                else
                {
                    throw new ArgumentException($"Conflicting option types: '{parsedType}' vs. '{name[end]}'."
                        , nameof(prototype));
                }

                AddSeparators(prototype, name, end, separators);
            }

            if (!parsedType.HasValue)
            {
                return null;
            }

            if (MaximumValueCount <= 1 && separators.Count != 0)
            {
                throw new ArgumentException(
                    "Cannot provide key/value separators for Options taking"
                    + $" {MaximumValueCount} value{(MaximumValueCount == 1 ? "" : "s")}."
                    , nameof(prototype));
            }

            // ReSharper disable once InvertIf
            if (MaximumValueCount > 1)
            {
                switch (separators.Count)
                {
                    case 0:
                        ValueSeparators = new[] {$"{Comma}"};
                        break;

                    case 1 when !IsNullOrEmpty(separators[0]):
                        ValueSeparators = null;
                        break;

                    default:
                        ValueSeparators = separators.ToArray();
                        break;
                }
            }

            return ReturnParsedType();
        }

        // ReSharper disable once UnusedParameter.Local
        /// <summary>
        /// Adds <paramref name="separators"/>.
        /// </summary>
        /// <param name="prototype"></param>
        /// <param name="name"></param>
        /// <param name="end"></param>
        /// <param name="separators"></param>
        private void AddSeparators(string prototype, string name, int end, ICollection<string> separators)
        {
            var start = -1;

            for (var i = end + 1; i < name.Length; ++i)
            {
                switch (name[i])
                {
                    case CurlyBracesOpen:

                        if (start != -1)
                        {
                            throw new ArgumentException($"Ill-formed name/value separator found in `{name}'."
                                , nameof(prototype));
                        }

                        start = i + 1;
                        break;

                    case CurlyBracesClose:

                        if (start == -1)
                        {
                            throw new ArgumentException($"Ill-formed name/value separator found in `{name}'."
                                , nameof(prototype));
                        }

                        separators.Add(name.Substring(start, i - start));
                        start = -1;
                        break;

                    default:

                        if (start == -1)
                        {
                            separators.Add($"{name[i]}");
                        }

                        break;
                }
            }

            if (start != -1)
            {
                throw new ArgumentException($"Ill-formed name/value separator found in `{name}'.", nameof(prototype));
            }
        }

        /// <summary>
        /// Invokes the Option given the <paramref name="context"/>.
        /// </summary>
        /// <param name="context"></param>
        public void Visit(OptionContext context) => OnVisitation(context);

        /// <summary>
        /// Occurs in an Option specific manner given <paramref name="context"/>.
        /// </summary>
        /// <param name="context"></param>
        protected abstract void OnVisitation(OptionContext context);

        public override string ToString() => Prototype;
    }
}
