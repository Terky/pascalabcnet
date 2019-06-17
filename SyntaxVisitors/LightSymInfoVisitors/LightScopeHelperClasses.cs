using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PascalABCCompiler.Parsers;

/*
    ProgramScopeSyntax
        BlockScopeSyntax
            ProcScopeSyntax (name)
                ParamsScopeSyntax
                    BlockScopeSyntax
                        StatListScopeSyntax (0)
                            StatListScopeSyntax (1)
            StatListScopeSyntax (0)
                StatListScopeSyntax (1)
*/

namespace PascalABCCompiler.SyntaxTree
{
    public enum SymKind
    {
        var, constant, field, param, procname, funcname, classname, recordname, interfacename,
        unitname, templatename, property, enumname, enumerator, typesynonym
    };

    [Flags]
    public enum Attributes { class_attr = 1, varparam_attr = 2, override_attr = 4, public_attr = 8 };

    public class SymInfoSyntax
    {
        public override string ToString()
        {
            string typepart = "";
            if (SK == SymKind.var || SK == SymKind.field || SK == SymKind.constant || SK == SymKind.param)
                typepart = ": " + (Td == null ? "NOTYPE" : Td.ToString());
            typepart = typepart.Replace("PascalABCCompiler.SyntaxTree.", "");
            var attrstr = Attr != 0 ? "[" + Attr.ToString() + "]" : "";
            var s = "(" + Id.ToString() + "{" + SK.ToString() + "}" + typepart + attrstr + ")" + $"({Pos.line}, {Pos.column})";
            return s;
        }

        public ident Id { get; set; }

        public type_definition Td { get; set; }

        public SymKind SK { get; set; }

        public Attributes Attr { get; set; }

        public Position Pos { get; set; }

        public SymInfoSyntax(ident Id, SymKind SK, Position Pos, type_definition Td = null, Attributes Attr = 0)
        {
            this.Id = Id;
            this.Td = Td;
            this.SK = SK;
            this.Attr = Attr;
            this.Pos = Pos;
        }

        public void AddAttribute(Attributes attr)
        {
            Attr |= attr;
        }
    }

    public class ScopeSyntax
    {
        public ScopeSyntax Parent { get; set; }

        public Position Pos { get; set; }

        public syntax_tree_node CorrespondingSyntaxTreeNode { get; }

        public HashSet<ScopeSyntax> Children = new HashSet<ScopeSyntax>();

        public HashSet<SymInfoSyntax> Symbols = new HashSet<SymInfoSyntax>();

        public ScopeSyntax(Position pos, syntax_tree_node correspondingSyntaxTreeNode)
        {
            Pos = pos;
            CorrespondingSyntaxTreeNode = correspondingSyntaxTreeNode;
        }

        public override string ToString() => GetType().Name.Replace("Syntax", "");
    }

    public class ScopeWithDefsSyntax : ScopeSyntax
    {
        public ScopeWithDefsSyntax(Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(pos, correspondingSyntaxTreeNode) { }
    } // 
    public class GlobalScopeSyntax : ScopeWithDefsSyntax // program_module unit_module
    {
        public GlobalScopeSyntax(Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(pos, correspondingSyntaxTreeNode) { }
    }

    public class ImplementationScopeSyntax : ScopeWithDefsSyntax
    {
        public ImplementationScopeSyntax(Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(pos, correspondingSyntaxTreeNode) { }
    }

    public class NamedScopeSyntax : ScopeWithDefsSyntax
    {
        public ident Name { get; set; }

        public NamedScopeSyntax(ident name, Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(pos, correspondingSyntaxTreeNode) => Name = name;

        public override string ToString() => base.ToString() + "(" + Name + ")" + $"({Pos.line} — {Pos.end_line})";
    }

    public class ProcScopeSyntax : NamedScopeSyntax // procedure_definition
    {
        public ident ClassName { get; set; }

        public ProcScopeSyntax(ident name, Position pos, syntax_tree_node correspondingSyntaxTreeNode, ident className = null)
            : base(name, pos, correspondingSyntaxTreeNode) => ClassName = className;
    }

    public class ParamsScopeSyntax : ScopeSyntax // formal_parameters
    {
        public ParamsScopeSyntax(Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(pos, correspondingSyntaxTreeNode) { }
    }

    public class TypeScopeSyntax : NamedScopeSyntax
    {
        public TypeScopeSyntax(ident name, Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(name, pos, correspondingSyntaxTreeNode) { }
    }

    public class ClassScopeSyntax : TypeScopeSyntax
    {
        public ClassScopeSyntax(ident name, Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(name, pos, correspondingSyntaxTreeNode) { }
    } // 

    public class RecordScopeSyntax : TypeScopeSyntax
    {
        public RecordScopeSyntax(ident name, Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(name, pos, correspondingSyntaxTreeNode) { }
    } // 

    public class InterfaceScopeSyntax : TypeScopeSyntax
    {
        public InterfaceScopeSyntax(ident name, Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(name, pos, correspondingSyntaxTreeNode) { }
    }

    public class EnumScopeSyntax : TypeScopeSyntax
    {
        public EnumScopeSyntax(ident name, Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(name, pos, correspondingSyntaxTreeNode) { }
    }

    public class TypeSynonymScopeSyntax : TypeScopeSyntax
    {
        public TypeSynonymScopeSyntax(ident name, Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(name, pos, correspondingSyntaxTreeNode) { }
    }

    public class LightScopeSyntax : ScopeSyntax // предок всех легковесных
    {
        public LightScopeSyntax(Position pos, syntax_tree_node correspondingSyntaxTreeNode) :
            base(pos, correspondingSyntaxTreeNode)
        { }

        public override string ToString()
        {
            var name = base.ToString();
            var level = 0;
            ScopeSyntax t = this;
            while (t.Parent is LightScopeSyntax)
            {
                level++;
                t = t.Parent;
            }
            return name + "(" + level + ")" + $"{Pos.line} {Pos.end_line}";
        }
    }
    public class StatListScopeSyntax : LightScopeSyntax // statement_list
    {
        public StatListScopeSyntax(Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(pos, correspondingSyntaxTreeNode) { }
    }

    public class RepeatScopeSyntax : LightScopeSyntax // statement_list
    {
        public RepeatScopeSyntax(Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(pos, correspondingSyntaxTreeNode) { }
    }

    public class CaseScopeSyntax : LightScopeSyntax // statement_list
    {
        public CaseScopeSyntax(Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(pos, correspondingSyntaxTreeNode) { }
    }

    public class ForScopeSyntax : LightScopeSyntax // statement_list
    {
        public ForScopeSyntax(Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(pos, correspondingSyntaxTreeNode) { }
    }

    public class ForeachScopeSyntax : LightScopeSyntax // statement_list
    {
        public ForeachScopeSyntax(Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(pos, correspondingSyntaxTreeNode) { }
    }

    public class IfScopeSyntax : LightScopeSyntax // statement_list
    {
        public IfScopeSyntax(Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(pos, correspondingSyntaxTreeNode) { }
    }

    public class WhileScopeSyntax : LightScopeSyntax // statement_list
    {
        public WhileScopeSyntax(Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(pos, correspondingSyntaxTreeNode) { }
    }

    public class LoopScopeSyntax : LightScopeSyntax // statement_list
    {
        public LoopScopeSyntax(Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(pos, correspondingSyntaxTreeNode) { }
    }

    public class WithScopeSyntax : LightScopeSyntax // statement_list
    {
        public WithScopeSyntax(Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(pos, correspondingSyntaxTreeNode) { }
    }

    public class LockScopeSyntax : LightScopeSyntax // statement_list
    {
        public LockScopeSyntax(Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(pos, correspondingSyntaxTreeNode) { }
    }
    public class SwitchScopeSyntax : LightScopeSyntax // statement_list
    {
        public SwitchScopeSyntax(Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(pos, correspondingSyntaxTreeNode) { }
    }
    public class LambdaScopeSyntax : ScopeSyntax
    {
        public LambdaScopeSyntax(Position pos, syntax_tree_node correspondingSyntaxTreeNode)
            : base(pos, correspondingSyntaxTreeNode) { }
    }
}
