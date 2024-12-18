using TermSAT.Formulas;

namespace TermSAT.NandReduction
{

    /**
     * An implementation of InstanceRepository (which is basically an implementation of a prefix tree) 
     * that saves the tree in a database, instead of in memory.  
     * A rule (ie noncanonical formula) in the database matches a given formula to reduce 
     * if the given formula is a substitution instance of the matching rule.
     * 
     * If its possible to generate a complete database of formulas for TermSAT 
     * then that set of formulas will require an index for efficiently matching the formulas in the 
     * database with a given formula to reduce.  
     * And, any such database will have at least 10s of millions fo rows.
     * And, any such index of that database will have a similar scale.  
     * And, any such index is *not* going to fit into RAM on any machine I can afford, therefore it must be stored a db.  
     * And finally, this class is required just to build the completed database, much less use it, 
     * otherwise I would not have wasted my time building this before actually creating the completed db, 
     * which has a low probability of success.
     * 
     * 
     * This repository uses a specially constructed database that stores the 
     * reduction rules as nodes in a trie.  
     * (@see com.googlecode.termsat.generator.RuleIndexer, the utility that 
     * creates the database for this class).
     * A trie structure is used to represent all the non-canonical formulas 
     * since a trie structure can be efficiently searched for matches.
     * 
     * Each node in the trie is a record in the database.
     * When searching for matching non-canonical formulas, this 
     * repository dynamically loads records from the database 
     * Dynamically loading records avoids the lengthy process of loading 
     * all the rules at once.
     * This can dramatically speed up the process of solving small problems 
     * since we can avoid loading most of the non-canonical formulas.
     * 
     * On the flip side, this class also uses soft references to reference nodes 
     * in the trie and so will release nodes when memory is low.  
     * This is good for large problems that tax the available memory.
     * 
     * This class works by overriding the TrieMap.createRoot method and 
     * returning a root node that gets all its children nodes from the 
     * rule index database.
     * 
     * @author Ted Stockwell
     * 
     * 
     * @TODO Add a MRU cache so that the most recently used nodes are never released.
     */
    public class RuleRepository : InstanceRecognizer
    {


        class RepositoryNode : TrieMap.NodeImpl<Formula>
        {

            readonly int _id;
            readonly int _canonicalID;

            override public Map<Character, Node<Formula>> getChildren()
            {
                if (_children == null)
                {
                    lock (this)
                    {
                        if (_children == null)
                        {
                            _children = loadChildren(this);
                        }
                    }
                }
                return super.getChildren();
            }

            protected TrieMap.NodeImpl<Formula> getChildNode(char symbol)
            {
                if (_children == null)
                {
                    lock (this)
                    {
                        if (_children == null)
                        {
                            _children = loadChildren(this);
                        }
                    }
                }
                return super.getChildNode(symbol);
            }

            RepositoryNode() { this(0, null, null, 0, 0); }
            RepositoryNode(int id, NodeImpl<Formula> parent, Character symbol, int depth, int canonicalID)
            {
                super(parent, symbol, depth);
                _id = id;
                _canonicalID = canonicalID;
            }

        }



        readonly Connection _connection;
        readonly HashMap<Integer, Formula> _canonicalFormulaCache = new HashMap<Integer, Formula>();

        readonly PreparedStatement _selectCanonicalFormula;
        readonly PreparedStatement _selectChildren;

        public RuleRepository()
        {
            super();
            try
            {
                // Load the JDBC driver
                Class.forName(driver);
                Properties props = new Properties();
                _connection = DriverManager.getConnection(dbURL, props);

                _selectCanonicalFormula = _connection.prepareStatement("SELECT CANONICAL.FORMULA FROM CANONICAL WHERE CANONICAL.ID = ?");

                _selectChildren = _connection.prepareStatement("SELECT * FROM NONCANONICAL WHERE PARENT = ?");

            }
            catch (ClassNotFoundException e)
            {
                throw new RuntimeException(e);
            }
            catch (SQLException e)
            {
                throw new RuntimeException(e);
            }
        }

        override protected TrieMap.NodeImpl<Formula> createRoot()
        {
            return new RepositoryNode();
        }

        private Map<Character, TrieMap.NodeImpl<Formula>> loadChildren(RepositoryNode node)
        {
            ArrayList<RepositoryNode> nodes = new ArrayList<RepositoryNode>();
            ResultSet rs = null;
            try
            {
                _selectChildren.setInt(1, node._id);
                rs = _selectChildren.executeQuery();
                while (rs.next())
                {
                    char symbol = rs.getString("SYMBOL").charAt(0);
                    int id = rs.getInt("ID");
                    int canonicalId = rs.getInt("CANONICAL_ID");
                    RepositoryNode rn = new RepositoryNode(id, node, symbol, node.depth() + 1, canonicalId);
                    if (canonicalId < 0)
                    {
                        String formulaText = "";
                        for (Node<Formula> p = node; p != null; p = p.getParent())
                        {
                            if (!p.isRoot())
                                formulaText = p.getChar() + formulaText;
                        }
                        formulaText += symbol;
                        Formula noncanonicalFormula = Formula.createFormula(formulaText);
                        Debug.Assert(noncanonicalFormula != null);
                        rn.setValue(noncanonicalFormula);
                    }
                    nodes.add(rn);
                }
                Map<Character, TrieMap.NodeImpl<Formula>> children = new HashMap<Character, TrieMap.NodeImpl<Formula>>();
                foreach (RepositoryNode rn in nodes)
                    children.put(rn.getCharacter(), rn);
                return children;
            }
            catch (SQLException e)
            {
                throw new RuntimeException(e);
            }
            finally
            {
                try { rs.close(); } catch (Throwable t) { }
            }
        }

        private Formula getCanonicalFormula(int formulaId)
        {
            try
            {
                Formula formula = _canonicalFormulaCache.get(formulaId);
                if (formula == null)
                {
                    ResultSet rs = null;
                    try
                    {
                        _selectCanonicalFormula.setInt(1, formulaId);
                        rs = _selectCanonicalFormula.executeQuery();
                        if (!rs.next())
                            throw new RuntimeException("Internal Error: no canonical formula with ID of " + formulaId);
                        String text = rs.getString(1);
                        formula = Formula.createFormula(text);
                        _canonicalFormulaCache.put(formulaId, formula);
                    }
                    finally
                    {
                        try { rs.close(); } catch (Throwable t) { }
                    }
                }
                return formula;
            }
            catch (SQLException e)
            {
                throw new RuntimeException(e);
            }
        }

        /**
         * Finds a reduced formula equivalent to the given formula
         * Returns null if the given formula cannot be reduced using the 
         * rules in the rule database.
         */
        public Formula findReducedFormula(Formula formula)
        {
            List<NodeInfo> matches = findMatchingNodes(formula, 1);
            if (matches == null)
                return null;
            NodeInfo info = matches.get(0);
            RepositoryNode rn = (RepositoryNode)info.node;
            Formula canonicalFormula = getCanonicalFormula(rn._canonicalID);
            return Formula.createInstance(canonicalFormula, info.substitutions);
        }


        /**
         * Finds all possible reduced formula equivalent to the given formula
         * Returns an empty container if the given formula cannot be reduced using the 
         * rules in the rule database.
         */
        public Collection<Formula> findAllReductions(Formula formula)
        {
            List<NodeInfo> matches = findMatchingNodes(formula, 1);
            if (matches == null || matches.isEmpty())
                return Collections.emptyList();
            ArrayList<Formula> reductions = new ArrayList<Formula>();
            foreach (NodeInfo info in matches)
            {
                RepositoryNode rn = (RepositoryNode)info.node;
                Formula canonicalFormula = getCanonicalFormula(rn._canonicalID);
                Formula reduction = Formula.createInstance(canonicalFormula, info.substitutions);
                reductions.add(reduction);
            }
            return reductions;
        }



    }

}
