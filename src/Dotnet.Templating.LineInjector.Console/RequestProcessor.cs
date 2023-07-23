using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.CodeAnalysis;

namespace Dotnet.Templating.LineInjector.Console
{
    public static class RequestProcessor
    {
        public static async Task<EitherAsync<Exception, Unit>> ProcessRequest(Request request)
        {
            var root = await GetRootSyntaxNode(request.PathToFile);

            return from target in SplitJsonPathToTarget(request.PathToFile).ToAsync()
                from classNode in FindClass(root, target.className).ToAsync()
                from methodNode in FindMethod(classNode, target.methodName).ToAsync()
                from returnStatement in FindReturnStatement(methodNode).ToAsync()
                from result in ReplaceReturnWithNewStatement(request, root, classNode, methodNode, returnStatement).ToAsync()
                select result;
        }

        private static async Task<SyntaxNode> GetRootSyntaxNode(string pathToFile)
        {
            var code = await File.ReadAllTextAsync(pathToFile);
            var tree = CSharpSyntaxTree.ParseText(code);

            return await tree.GetRootAsync();
        }

        private static Either<Exception, (string className, string methodName)> SplitJsonPathToTarget(string jsonPathToTarget)
        {
            var targetPathParts = jsonPathToTarget.Split('.');
            if (targetPathParts.Length != 2)
            {
                return new Exception($"PathToMethod is not correct: {jsonPathToTarget}. Should contain class.method");
            }

            return (targetPathParts.First(), targetPathParts.Last());
        }

        private static Either<Exception, ClassDeclarationSyntax> FindClass(SyntaxNode root, string className)
        {
            var classNode = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.Text == className);

            if (classNode == null)
            {
                return new Exception($"Cant find class {className} by name.");
            }

            return classNode;
        }

        private static Either<Exception, MethodDeclarationSyntax> FindMethod(ClassDeclarationSyntax classNode, string methodName)
        {
            var methodNode = classNode.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == methodName);

            if (methodNode == null)
            {
                return new Exception($"Cant find method {methodName} in class {classNode.Identifier.Text}.");
            }

            return methodNode;
        }

        private static Either<Exception, ReturnStatementSyntax> FindReturnStatement(MethodDeclarationSyntax methodNode)
        {
            var returnStatement = methodNode.DescendantNodes()
                .OfType<ReturnStatementSyntax>()
                .FirstOrDefault();

            if (returnStatement == null)
            {
                return new Exception($"Cant find return in {methodNode.Identifier.Text} method");
            }

            return returnStatement;
        }

        private static async Task<Either<Exception, Unit>> ReplaceReturnWithNewStatement(
            Request request,
            SyntaxNode root,
            ClassDeclarationSyntax classNode,
            MethodDeclarationSyntax methodNode,
            ReturnStatementSyntax returnStatement)
        {
            try
            {
                var newStatement = SyntaxFactory.ParseStatement(request.LineToPaste)
                    .WithLeadingTrivia(returnStatement.GetLeadingTrivia())
                    .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

                var newReturnStatement = returnStatement.WithLeadingTrivia(returnStatement.GetLeadingTrivia());
                var newNode = methodNode.ReplaceNode(returnStatement, new[] { newStatement, newReturnStatement });
                var newClassNode = classNode.ReplaceNode(methodNode, newNode);
                var newRoot = root.ReplaceNode(classNode, newClassNode);

                await File.WriteAllTextAsync(request.PathToFile, newRoot.ToFullString());
                return Unit.Default;
            }
            catch (Exception e)
            {
                return e;
            }
        }
    }
}