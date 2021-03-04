using HarmonyLib;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using System;

namespace SmartBepInMods.Tools.Transpiling
{
    /// <summary>
    /// Extentions for all the stuff.
    /// </summary>
    public static class StaticClassExtensions
    {
        public static bool DEBUG = false;
        /// <summary>
        /// A FX 3.5 way to mimic the FX4 "HasFlag" method.
        /// </summary>
        /// <param name="variable">The tested enum.</param>
        /// <param name="value">The value to test.</param>
        /// <returns>True if the flag is set. Otherwise false.</returns>
        internal static bool HasFlag(this Enum variable, Enum value)
        {
            // check if from the same type.
            if (variable.GetType() != value.GetType())
            {
                throw new ArgumentException("The checked flag is not from the same type as the checked variable.");
            }

            Convert.ToUInt64(value);
            ulong num = Convert.ToUInt64(value);
            ulong num2 = Convert.ToUInt64(variable);

            return (num2 & num) == num;
        }
        /// <summary>
        /// A FX 3.5 way to get all flags within the variable.
        /// </summary>
        /// <param name="variable">The flags enum.</param>
        /// <returns></returns>
        internal static IEnumerable<Enum> GetFlags(this Enum variable)
        {
            foreach (Enum value in Enum.GetValues(variable.GetType()))
            {
                if (variable.HasFlag(value)) yield return value;
            }
        }
        /// <summary>
        /// Delegate for substituting the match.
        /// </summary>
        /// <param name="CutSource">The match IL code.</param>
        /// <returns></returns>
        public delegate IEnumerable<CodeInstruction> MatchFound(IEnumerable<CodeInstruction> CutSource);
        /// <summary>
        /// Searches for all matches and executes the callback method.
        /// </summary>
        /// <param name="search">List of instructions to search(this).</param>
        /// <param name="match">List of instructions w/ qualifiers to match to.</param>
        /// <param name="Found">Callback delegate for replacing matches.</param>
        /// <param name="log">Log item.</param>
        /// <returns>The instructions after all callbacks have completed.</returns>
        public static IEnumerable<CodeInstruction> SmartMatch(this IEnumerable<CodeInstruction> search, IEnumerable<KeyValuePair<CodeInstruction, Qualifier>> match, MatchFound Found, Log log)
        {
            log("[SmartTranspiler] Attempting smart match");
            var doLog = log != null;
            var array1 = search.ToArray(); // Search
            var array2 = match.ToArray(); // Match
            var FINAL = new List<CodeInstruction>(); // FINAL

            var found = 0;
            var offset = 0;
            var stack = new List<CodeInstruction>();
            // Do the scanning :D
            for (int i = 0; i < array1.Length; i++)
            {
                // Check if fully validated
                if (offset == array2.Length)
                {
                    log($"[SmartTranspiler] Found IL Match @ {i - stack.Count}.");
                    if (FINAL.Count > 0 && DEBUG)
                        {
                            log($"--{i - stack.Count - 1} \t{FINAL[FINAL.Count - 1]}");
                            for (var w = 0; w < stack.Count; w++)
                            {
                                log($"{i - stack.Count + w} \t{stack[w]}");
                            }
                        }
                    // It's a full match, call back and add the result to the FINAL
                    var replaces = Found(stack);
                    FINAL.AddRange(replaces);

                    if (DEBUG)
                    {
                        log("[SmartTranspiler] New IL Code");
                        for (var w = 0; w < replaces.ToArray().Length; w++)
                        {
                            log($"{i - replaces.ToArray().Length + w} \t{replaces.ToArray()[w]}");
                        }
                    }

                    // Reset for the next match
                    stack.Clear();
                    offset = 0;
                    found++;
                }

                var item1 = array1[i]; // Item From
                var item2 = array2[offset]; // Match To

                bool qualifies = true;
                Action BreaksQualification = () => qualifies = false; ;

                // Validate all flags to ensure it qualifies
                foreach (Enum flag in item2.Value.GetFlags())
                {
                    switch (flag)
                    {
                        case Qualifier.OpCode:
                            // Special cases, soft break
                            if ((item1.opcode == OpCodes.Brfalse || item1.opcode == OpCodes.Brfalse_S) && (item2.Key.opcode == OpCodes.Brfalse || item2.Key.opcode == OpCodes.Brfalse_S)) break;
                            if ((item1.opcode == OpCodes.Brtrue || item1.opcode == OpCodes.Brtrue_S) && (item2.Key.opcode == OpCodes.Brtrue || item2.Key.opcode == OpCodes.Brtrue_S)) break;
                            // Heard break
                            if (item1.opcode != item2.Key.opcode) BreaksQualification();
                            break;
                        case Qualifier.Operand:
                            if (item1.operand != item2.Key.operand) BreaksQualification();
                            break;
                        case Qualifier.Labels:
                            var breaksLabels = false;
                            var i2 = 0;
                            foreach (var lab in item1.labels)
                            {
                                // Check if out of index
                                if (i2 >= item2.Key.labels.Count)
                                {
                                    breaksLabels = true;
                                    break;
                                }
                                // Check if Label matches
                                if (item1.labels[i2] != item2.Key.labels[i2])
                                {
                                    breaksLabels = true;
                                    break;
                                }

                                i2++;
                            }
                            if (breaksLabels) BreaksQualification();
                            break;
                        case Qualifier.Blocks:
                            var breaksBlocks = false;
                            var i3 = 0;
                            foreach (var block in item1.blocks)
                            {
                                // Check if out of index
                                if (i3 >= item2.Key.blocks.Count)
                                {
                                    breaksBlocks = true;
                                    break;
                                }
                                // Check if Blocks matches
                                if (item1.blocks[i3].blockType != item2.Key.blocks[i3].blockType)
                                {
                                    breaksBlocks = true;
                                    break;
                                }
                                if (item1.blocks[i3].catchType != item2.Key.blocks[i3].catchType)
                                {
                                    breaksBlocks = true;
                                    break;
                                }

                                i3++;
                            }
                            if (breaksBlocks) BreaksQualification();
                            break;
                    }
                }
                if (qualifies)
                {
                    // This does qualify so add it to the stack
                    stack.Add(item1);
                    offset++;
                }
                else
                {
                    // This didn't match so dump our stack, doesn't qualify
                    FINAL.AddRange(stack);
                    stack.Clear();
                    offset = 0;

                    // This one doesn't match so add it to the list
                    FINAL.Add(item1);
                }
            }

            if (found == 0)
            {
                log("[SmartTranspiler] Failed to find a match!");
                return search;
            }else
            {
                log($"[SmartTranspiler] {found} match{(found > 1 ? "es" : "")} found & handled");
            }

            return FINAL;
        }
        /// <summary>
        /// Replaces all of the match with the provided <b>replacement</b>.
        /// </summary>
        /// <param name="search">Items to search(this).</param>
        /// <param name="match">Items to match to.</param>
        /// <param name="replacement">Replacement instructions.</param>
        /// <param name="log">Log item.</param>
        /// <returns>The edited instructions.</returns>
        public static IEnumerable<CodeInstruction> SmartReplaceAll(this IEnumerable<CodeInstruction> search, IEnumerable<KeyValuePair<CodeInstruction, Qualifier>> match, IEnumerable<CodeInstruction> replacement, Log log)
        {
            return search.SmartMatch(match, (Source) =>
            {
                return replacement;
            }, log);
        }
        /// <summary>
        /// Replaces each instruction in the match with a NOP;
        /// </summary>
        /// <param name="search">This :D</param>
        /// <param name="match">Instructions to match with qualifiers.</param>
        /// <param name="log">Log item.</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> SmartNopAll(this IEnumerable<CodeInstruction> search, IEnumerable<KeyValuePair<CodeInstruction, Qualifier>> match, Log log)
        {
            return search.SmartMatch(match, (Source) =>
            {
                return new CodeInstruction[1] { new CodeInstruction(OpCodes.Nop) };
            }, log);
        }
        /// <summary>
        /// Adds the IL code to the end of the match.
        /// </summary>
        /// <param name="search">this</param>
        /// <param name="match">The IL code to match.</param>
        /// <param name="addition">The IL code to add.</param>
        /// <param name="log">Logger.</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> SmartPostfix(this IEnumerable<CodeInstruction> search, IEnumerable<KeyValuePair<CodeInstruction, Qualifier>> match, IEnumerable<CodeInstruction> addition, Log log)
        {
            return search.SmartMatch(match, (Source) =>
            {
                var x = Source.ToList();
                x.AddRange(addition.ToList());
                return x;
            }, log);
        }
        /// <summary>
        /// Adds the IL code to the beginning of the match.
        /// </summary>
        /// <param name="search">this</param>
        /// <param name="match">The IL code to match.</param>
        /// <param name="addition">The IL code to add.</param>
        /// <param name="log">Logger.</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> SmartPrefix(this IEnumerable<CodeInstruction> search, IEnumerable<KeyValuePair<CodeInstruction, Qualifier>> match, IEnumerable<CodeInstruction> addition, Log log)
        {
            return search.SmartMatch(match, (Source) =>
            {
                var x = addition.ToList();
                x.AddRange(Source.ToList());
                return x;
            }, log);
        }
        /// <summary>
        /// Convience function for adding a qualifier
        /// </summary>
        /// <param name="cI">this</param>
        /// <param name="qualifier">Qualifier.</param>
        /// <returns></returns>
        public static KeyValuePair<CodeInstruction, Qualifier> Qualify(this CodeInstruction cI, Qualifier qualifier)
        {
            return new KeyValuePair<CodeInstruction, Qualifier>(cI, qualifier);
        }
        
    }
    /// <summary>
    /// Qualifier for CodeInstructions to match.
    /// </summary>
    public enum Qualifier
    {
        /// <summary>
        /// Matches the OpCode of this <b>CodeInstruction</b>
        /// </summary>
        OpCode = 1,
        /// <summary>
        /// Matches the Operand of this <b>CodeInstruction</b>
        /// </summary>
        Operand = 2,
        /// <summary>
        /// Matches the Labels of this <b>CodeInstruction</b>
        /// </summary>
        Labels = 4,
        /// <summary>
        /// Matches the Blocks of this <b>CodeInstruction</b>
        /// </summary>
        Blocks = 8,

        /// <summary>
        /// Only matches the OpCode of the <b>CodeInstruction</b>, the only consistently !null value.
        /// </summary>
        Default = OpCode,

    }
}
