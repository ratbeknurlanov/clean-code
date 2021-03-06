﻿using System.Collections.Generic;
using System.Linq;
using Markdown.ParserClasses;
using Markdown.TokenizerClasses.Scanners;

namespace Markdown.TokenizerClasses
{
    public class Tokenizer
    {
        private readonly List<IScanner> tokenScanners = new List<IScanner>
        {
            new TagScanner(),
            new PlainTextScanner()
        };

        public Deque<Token> Tokenize(string text)
        {
            var tokens = new Deque<Token>();
            Token token;

            do
            {
                token = GetNextToken(text);
                if (tokens.Count > 0)
                {
                    var previousToken = tokens.Last();

                    if (CheckDoubleUnderscore(token, previousToken, out var newToken)
                        || CheckConsecutiveNumbers(token, previousToken, out newToken)
                        || CheckEscapedTokens(token, previousToken, out newToken))
                    {
                        token = newToken;
                        tokens.RemoveBack();
                    }

                    if (CheckUnderscoreBetweenNumbers(token, tokens, out newToken))
                    {
                        tokens.RemoveBack();
                        tokens.Add(newToken);
                    }
                }

                tokens.Add(token);

                if (token.Length > text.Length)
                    break;

                text = text.Substring(token.Length, text.Length - token.Length);
            } while (token.Type != TokenType.EOF);

            RemoveEOFToken(tokens);

            return tokens;
        }

        public Token GetNextToken(string text)
        {
            foreach (var scanner in tokenScanners)
                if (scanner.TryScan(text, out var token))
                    return token;

            return Token.EOF;
        }

        private static void RemoveEOFToken(Deque<Token> tokens) => tokens.RemoveBack();

        private bool CheckDoubleUnderscore(Token currentToken, Token previousToken, out Token newToken)
        {
            if (currentToken.Type == TokenType.Underscore && previousToken.Type == TokenType.Underscore)
            {
                newToken = new Token(TokenType.DoubleUnderscore, "__");
                return true;
            }

            newToken = currentToken;
            return false;
        }

        private bool CheckConsecutiveNumbers(Token currentToken, Token previousToken, out Token newToken)
        {
            if (currentToken.Type == TokenType.Num && previousToken.Type == TokenType.Num)
            {
                newToken = new Token(TokenType.Num, previousToken.Value + currentToken.Value);
                return true;
            }

            newToken = currentToken;
            return false;
        }

        private bool CheckEscapedTokens(Token currentToken, Token previousToken, out Token newToken)
        {
            if ((currentToken.Type == TokenType.Underscore
                 || currentToken.Type == TokenType.DoubleUnderscore
                 || currentToken.Type == TokenType.EscapeChar)
                && previousToken.Type == TokenType.EscapeChar)
            {
                newToken = new Token(TokenType.Text, currentToken.Value);
                return true;
            }

            newToken = currentToken;
            return false;
        }

        private bool CheckUnderscoreBetweenNumbers(Token currentToken, Deque<Token> tokens, out Token newToken)
        {
            if (tokens.Count >= 2)
            {
                var previousToken = tokens.Last();
                var penultimateToken = tokens.Penultimate();

                if (currentToken.Type == TokenType.Num
                    && (previousToken.Type == TokenType.Underscore || previousToken.Type == TokenType.DoubleUnderscore)
                    && penultimateToken.Type == TokenType.Num)
                {
                    newToken = new Token(TokenType.Text, previousToken.Value);
                    return true;
                }
            }

            newToken = null;
            return false;
        }
    }
}