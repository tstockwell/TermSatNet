using System.ComponentModel.DataAnnotations;

namespace TermSAT.Formulas;


public static partial class FormulaIndex
{
    public class Node
    {
        /// <summary>
        /// The root node always has an index of 0
        /// </summary>
        [Key]
        public int Id { get; private set; }
        public const int ROOT_ID = 0;


        /// <summary>
        /// The index of the parent node in the index
        /// -1 for the root node
        /// </summary>
        [Required]
        public int Parent { get; set; }
        public const int PARENT_NONE = -1;

        /// <summary>
        /// A key that identifies the next symbol in a formula's flat-term
        /// 
        /// -3 == root of trie, the root has no value but many branches
        /// -2 == nand operator
        /// -1 == Constant.TRUE
        /// 0 == Constant.FALSE
        /// other == variable number
        /// </summary>
        [Required]
        public int Key { get; set; }
        public const int KEY_ROOT = -3;
        public const int KEY_FALSE = -2;
        public const int KEY_TRUE = -1;
        public const int KEY_NAND = 0;

        /// <summary>
        /// The Id of the corresponding formula in the 'rule database'.
        /// -1 == n/a, no value
        /// 
        /// There shouldn't be any leaves without values.  
        /// The root of the trie does not have a value.
        /// </summary>
        [Required]
        public long Value { get; set; }
        public const long VALUE_NONE = -1;

        public Node(int parent, int key, long value)
        {
            Parent = parent;
            Key = key;
            Value = value;
        }
        private Node()
        {
        }
    }

}
