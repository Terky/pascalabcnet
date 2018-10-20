﻿// Copyright (c) Ivan Bondarev, Stanislav Mihalkovich (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PascalABCCompiler.SyntaxTree;
using SyntaxVisitors;
using SyntaxVisitors.SugarVisitors;

namespace PascalABCCompiler.SyntaxTreeConverters
{
    public class StandardSyntaxTreeConverter: ISyntaxTreeConverter
    {
        public string Name { get; } = "Standard";
        public syntax_tree_node Convert(syntax_tree_node root)
        {
            CapturedNamesHelper.Reset();
            // Прошивание ссылками на Parent nodes. Должно идти первым
            // FillParentNodeVisitor расположен в SyntaxTree/tree как базовый визитор, отвечающий за построение дерева
            FillParentNodeVisitor.New.ProcessNode(root);

            // Выносим выражения с лямбдами из заголовка foreach
            StandOutExprWithLambdaInForeachSequenceVisitor.New.ProcessNode(root);

            // type classes - пока закомментировал SSM 20/10/18. Грязный кусок кода. FillParentsInAllChilds вызывается повторно

            /*{
                var typeclasses = SyntaxVisitors.TypeclassVisitors.FindTypeclassesVisitor.New;
                typeclasses.ProcessNode(root);
                var instancesAndRestrictedFunctions = SyntaxVisitors.TypeclassVisitors.FindInstancesAndRestrictedFunctionsVisitor.New(typeclasses.typeclasses);
                instancesAndRestrictedFunctions.ProcessNode(root);
                SyntaxVisitors.TypeclassVisitors.ReplaceTypeclassVisitor.New(instancesAndRestrictedFunctions).ProcessNode(root);
            }
            root.FillParentsInAllChilds();*/
#if DEBUG
            //new SimplePrettyPrinterVisitor("E:/projs/out.txt").ProcessNode(root);
#endif
            // loop
            LoopDesugarVisitor.New.ProcessNode(root);

            // tuple_node
            TupleVisitor.New.ProcessNode(root);

            // assign_tuple и assign_var_tuple
            AssignTuplesDesugarVisitor.New.ProcessNode(root);

            // slice_expr и slice_expr_question
            SliceDesugarVisitor.New.ProcessNode(root);

            // question_point_desugar_visitor
            QuestionPointDesugarVisitor.New.ProcessNode(root);

            // double_question_desugar_visitor
            DoubleQuestionDesugarVisitor.New.ProcessNode(root);

            // Patterns
            SingleDeconstructChecker.New.ProcessNode(root);
            PatternsDesugaringVisitor.New.ProcessNode(root);

            // Всё, связанное с yield
            MarkMethodHasYieldAndCheckSomeErrorsVisitor.New.ProcessNode(root);
            ProcessYieldCapturedVarsVisitor.New.ProcessNode(root);

#if DEBUG
            //new SimplePrettyPrinterVisitor("D:\\Tree.txt").ProcessNode(root);
            //FillParentNodeVisitor.New.ProcessNode(root);

            
            /*var cv = CollectLightSymInfoVisitor.New;
            cv.ProcessNode(root);
            cv.Output(@"Light1.txt");*/
            
            /*try
            {
                root.visit(new SimplePrettyPrinterVisitor(@"d:\\zzz4.txt"));
            }
            catch(Exception e)
            {

                System.IO.File.AppendAllText(@"d:\\zzz4.txt",e.Message);
            }*/

#endif
            return root;
        }
    }
}
