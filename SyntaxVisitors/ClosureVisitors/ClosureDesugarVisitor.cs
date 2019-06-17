using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PascalABCCompiler.SyntaxTree;
using PascalABCCompiler.TreeConverter;
using PascalABCCompiler.Errors;

namespace SyntaxVisitors.ClosureVisitors
{

    public class ClosureDesugarVisitor: BaseChangeVisitor
    {
        private syntax_tree_node programRoot = null;

        private static declarations programDeclarations = null;

        //public static ClosureDesugarVisitor New
        //{
        //    get { return new ClosureDesugarVisitor(); }
        //}

        public ClosureDesugarVisitor(syntax_tree_node programRoot)
        {
            this.programRoot = programRoot;
        }

        private long lambdaNum = 0;

        private string getUniqueLambdaClassName()
        {
            return "<>lambda_class" + lambdaNum++;
        }

        public override void visit(declarations declarations)
        {
            if (declarations.Parent.Parent is program_module || declarations.Parent is interface_node)
            {
                programDeclarations = declarations;
            }
            base.visit(declarations);
        }

        // TODO: переписать чтобы можно было понять что именно в параметрах не сошлось
        //private bool checkLambdaParameters(formal_parameters parametersFromType,
        //    formal_parameters parametersFromLambdaDefinition)
        //{
        //    if (parametersFromType == null)
        //    {
        //        return parametersFromLambdaDefinition == null;
        //    }
        //    else if (parametersFromLambdaDefinition == null)
        //    {
        //        return parametersFromType == null;
        //    }
        //    var typeParameters = parametersFromType.params_list;
        //    var lambdaParameters = parametersFromLambdaDefinition.params_list;
        //    if (typeParameters.Count == lambdaParameters.Count)
        //    {
        //        for (int i = 0; i < parametersFromType.Count; ++i)
        //        {
        //            if (!lambdaParameters[i].vars_type.ToString().Equals(typeParameters[i].vars_type.ToString()) ||
        //                !lambdaParameters[i].idents.list[0].name.Equals(typeParameters[i].idents.list[0].name))
        //            {
        //                return false;
        //            }
        //        }
        //    }
        //    return true;
        //}

        public override void visit(function_lambda_definition functionLambdaDefinition)
        {
            var lambdaMethod = new procedure_definition();

            var lambdaMethodParameters = functionLambdaDefinition.formal_parameters;

            //if (functionLambdaDefinition.Parent is var_def_statement statement)
            //{
            //    //if (!checkLambdaParameters((statement.vars_type as procedure_header).parameters, lambdaMethodParameters))
            //    //    throw new SyntaxError("Lambda parameters are not valid", statement);
            //    lambdaMethodParameters = (statement.vars_type as procedure_header).parameters;
            //}
            //else if (functionLambdaDefinition.Parent is expression_list expressionList)
            //{
            //    //if (expressionList.Parent is method_call methodCall)
            //    //{
            //    //    methodCall.p
            //    //    throw new SyntaxError("Guessed it!", functionLambdaDefinition.Parent);
            //    //}
            //    //return;
            //}

            if ((functionLambdaDefinition.Parent as var_def_statement)?.vars_type is function_header functionHeader)
            {
                function_header lambdaMethodHeader = null;
                if (functionLambdaDefinition.return_type is lambda_inferred_type)
                {
                    lambdaMethodHeader = new function_header(functionLambdaDefinition.lambda_name,
                        functionHeader.return_type, functionHeader.parameters);
                }
                else
                {
                    lambdaMethodHeader = new function_header(functionLambdaDefinition.lambda_name,
                        functionLambdaDefinition.return_type, lambdaMethodParameters);
                }
                lambdaMethodHeader.source_context = functionLambdaDefinition.source_context;
                lambdaMethod.proc_header = lambdaMethodHeader;
            }
            else
            {
                var lambdaMethodHeader = new procedure_header(functionLambdaDefinition.lambda_name,
                    lambdaMethodParameters, null);
                lambdaMethodHeader.source_context = functionLambdaDefinition.source_context;
                lambdaMethod.proc_header = lambdaMethodHeader;
            }
            
            lambdaMethod.proc_body = new block(functionLambdaDefinition.proc_body as statement_list);

            var lambdaClassMembers = new class_members(access_modifer.public_modifer);
            lambdaClassMembers.Add(lambdaMethod);

            //var lambdaClassBody = new class_body_list();
            //lambdaClassBody.Add(lambdaClassMembers);

            var lambdaClass = SyntaxTreeBuilder.BuildClassDefinition(lambdaClassMembers);
            //lambdaClass.body = lambdaClassBody;
            lambdaClass.source_context = functionLambdaDefinition.source_context;

            var lambdaClassIdent = new ident(getUniqueLambdaClassName(), functionLambdaDefinition.source_context);
            var lambdaClassDeclaration = new type_declaration(lambdaClassIdent, lambdaClass);
            var typeDeclarations = new type_declarations();
            typeDeclarations.Add(lambdaClassDeclaration, functionLambdaDefinition.source_context);

            var variablesCapturer = new CaptureVariablesVisitor(functionLambdaDefinition);//.New;
            variablesCapturer.ProcessNode(functionLambdaDefinition);
            var capturedVariables = variablesCapturer.CapturedIdents;

            var lambdaClassConstructorParameters = new expression_list();
            var constructorFormalParams = new formal_parameters();
            var constructorBody = new statement_list();
            constructorBody.source_context = functionLambdaDefinition.source_context;
            foreach (var id in capturedVariables)
            {
                var idents = new ident_list(id.Key.Id);
                var classField = new var_def_statement(idents, id.Key.Td, functionLambdaDefinition.source_context);
                lambdaClassMembers.Add(classField);
                lambdaClassConstructorParameters.Add(id.Value as expression);
                constructorFormalParams.Add(new typed_parameters(id.Key.Id, id.Key.Td));
                var initStatement = new assign(new dot_node(new ident("self"), id.Key.Id), id.Key.Id,
                    Operators.Assignment, functionLambdaDefinition.source_context);
                constructorBody.Add(initStatement);
            }
            if (lambdaClassConstructorParameters.Count != 0)
            {
                var lambdaClassConstructor = new procedure_definition(new constructor(constructorFormalParams, functionLambdaDefinition.source_context),
                    new block(constructorBody), functionLambdaDefinition.source_context);
                lambdaClassMembers.Add(lambdaClassConstructor);
            }

            if (programDeclarations != null)
            {
                programDeclarations.AddFirst(typeDeclarations);
                //(functionLambdaDefinition.Parent as var_def_statement).vars_type = null;
                ReplaceUsingParent(functionLambdaDefinition, new dot_node(
                    new method_call(new dot_node(lambdaClassIdent.name, new ident("Create")),
                                    lambdaClassConstructorParameters,
                                    functionLambdaDefinition.source_context),
                    lambdaMethod.proc_header.name.meth_name));
                //visit(lambdaClassDeclaration);
            }
            else
            {
                throw new NullReferenceException("programDeclarations was null");
            }
            base.visit(functionLambdaDefinition);
        }

        public override void ProcessNode(syntax_tree_node Node)
        {
            if (programRoot is program_module)
            {
                programRoot = Node;
            }
            if (Node != null)
            {
                base.ProcessNode(Node);
                //Console.WriteLine(Node.ToString());
            }

        }
    }
}
