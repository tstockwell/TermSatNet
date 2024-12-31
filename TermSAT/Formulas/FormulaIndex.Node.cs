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
        /// The parent node in the index
        /// -1 for the root node
        /// </summary>
        [Required]
        public int Parent { get; set; }
        public const int PARENT_NONE = -1;

        /// <summary>
        /// -1 == root
        /// 0 == nand operator
        /// other == variable number
        /// </summary>
        [Required]
        public int Key { get; set; }
        public const int KEY_NAND = 0;
        public const int KEY_ROOT = -1;

        /// <summary>
        /// The Id of the corresponding formula in the 'rule database'.
        /// -1 == n/a
        /// </summary>
        [Required]
        public int Value { get; set; }
        public const int VALUE_NONE = -1;

        public Node(int parent, int key, int value)
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
