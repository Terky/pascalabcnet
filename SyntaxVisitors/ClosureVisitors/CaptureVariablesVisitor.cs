using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PascalABCCompiler.SyntaxTree;
using PascalABCCompiler.Parsers;

namespace SyntaxVisitors.ClosureVisitors
{
    public class CaptureVariablesVisitor: BaseChangeVisitor
    {
        public class CapturedIdent: SymInfoSyntax
        {
            public ScopeSyntax DefScope { get; set; }

            CapturedIdent(ident id, PascalABCCompiler.SyntaxTree.SymKind symbolKind, Position pos, ScopeSyntax defScope = null,
                type_definition typeDef = null, Attributes attr = 0) : base(id, symbolKind, pos, typeDef, attr)
            {
                DefScope = defScope;
            }
        }

        //public static CaptureVariablesVisitor New => new CaptureVariablesVisitor();

        private function_lambda_definition currentLambda = null;

        public CaptureVariablesVisitor(function_lambda_definition currentLambda)
        {
            this.currentLambda = currentLambda;
        }

        private Dictionary<SymInfoSyntax, syntax_tree_node> capturedIdents = new Dictionary<SymInfoSyntax, syntax_tree_node>();

        public Dictionary<SymInfoSyntax, syntax_tree_node> CapturedIdents { get { return capturedIdents; } }

        private ScopeSyntax lightSymbolTableCurrentScope = null;

        private Dictionary<string, List<SymInfoSyntax>> allIdentsFromTopScopes = new Dictionary<string, List<SymInfoSyntax>>();

        private ScopeSyntax findCurrentScope(syntax_tree_node node, ScopeSyntax lightSymbolTableRoot)
        {
            var queue = new Queue<ScopeSyntax>();
            queue.Enqueue(lightSymbolTableRoot);

            while (queue.Count != 0)
            {
                var currentScope = queue.Dequeue();
                if (currentScope.CorrespondingSyntaxTreeNode == node)
                {
                    return currentScope;
                }
                foreach (var internalScope in currentScope.Children)
                {
                    queue.Enqueue(internalScope);
                }
            }
            return null;
        }

        private ScopeSyntax findClassScope(string className, ScopeSyntax lightSymbolTableRoot)
        {
            var queue = new Queue<ScopeSyntax>();
            queue.Enqueue(lightSymbolTableRoot);

            while (queue.Count != 0)
            {
                var currentScope = queue.Dequeue();
                if (currentScope is ClassScopeSyntax classScope && classScope.Name.name == className)
                {
                    return currentScope;
                }
                foreach (var internalScope in currentScope.Children)
                {
                    queue.Enqueue(internalScope);
                }
            }
            return null;
        }

        private void collectSingleScopeIdents(ScopeSyntax currentScope, bool replacingIdents)
        {
            foreach (var ident in currentScope.Symbols)
            {
                if (!allIdentsFromTopScopes.ContainsKey(ident.Id.name))
                {
                    allIdentsFromTopScopes.Add(ident.Id.name, new List<SymInfoSyntax>());
                    allIdentsFromTopScopes[ident.Id.name].Add(ident);
                }
                else if (replacingIdents)
                {
                    allIdentsFromTopScopes[ident.Id.name].Add(ident);
                }
            }
        }

        private void collectIdents(ScopeSyntax currentScope)
        {
            while (currentScope.Parent != null)
            {
                collectSingleScopeIdents(currentScope.Parent, false);
                currentScope = currentScope.Parent;
            }
        }

        public override void visit(ident ident)
        {
            var type = ident.Parent.GetType();
            if (allIdentsFromTopScopes.ContainsKey(ident.name))
            {
                var elem = allIdentsFromTopScopes[ident.name].Last();
                if (!capturedIdents.ContainsKey(elem))
                {
                    capturedIdents.Add(elem, new ident(ident.name, ident.source_context));
                    var new_elem = allIdentsFromTopScopes[ident.name].First();
                    if ((new_elem.SK == PascalABCCompiler.SyntaxTree.SymKind.funcname ||
                        new_elem.SK == PascalABCCompiler.SyntaxTree.SymKind.procname) &&
                        ident.Parent is method_call methodCall)
                    {
                        capturedIdents.Remove(elem);
                        capturedIdents.Add(new_elem, new method_call(ident, methodCall.parameters,
                            methodCall.source_context));
                        ReplaceUsingParent(methodCall, new ident(ident.name));
                    }
                }
            }
        }

        public override void ProcessNode(syntax_tree_node Node)
        {
            if (Node == currentLambda)
            {
                while (Node.Parent != null)
                {
                    Node = Node.Parent;
                }

                var lightSymbolTableCollector = CollectLightSymInfoVisitor.New;
                lightSymbolTableCollector.ProcessNode(Node);
                lightSymbolTableCurrentScope = findCurrentScope(currentLambda, lightSymbolTableCollector.Root);
                
                collectIdents(lightSymbolTableCurrentScope);
                if (lightSymbolTableCurrentScope.Parent.Parent is ProcScopeSyntax procScope && procScope.ClassName != null)
                {

                    collectSingleScopeIdents(findClassScope(procScope.ClassName.name, lightSymbolTableCollector.Root), true);
                }

                Node = currentLambda;
            }
            base.ProcessNode(Node);
        }
    }
}
