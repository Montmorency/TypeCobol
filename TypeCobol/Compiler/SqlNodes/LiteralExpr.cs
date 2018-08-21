﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TypeCobol.Compiler.SqlNodes.Catalog;

namespace TypeCobol.Compiler.SqlNodes
{
    /**
     * Representation of a literal expression. Literals are comparable to allow
     * ordering of HdfsPartitions whose partition-key values are represented as literals.
     */
    public class LiteralExpr : Expr
    {
        public override string ToSqlImpl()
        {
            return base.toSql();
        }

        public override string DebugString()
        {
            return base.debugString();
        }

        public override bool LocalEquals(Expr that)
        {
            return base.localEquals(that);
        }

        public override void ResetAnalysisState()
        {
            base.resetAnalysisState();
        }

        protected override bool IsConstantImpl()
        {
            return false;
        }


        public LiteralExpr()
        {
            evalCost_ = LITERAL_COST;
            numDistinctValues_ = 1;
        }

        /**
         * Copy c'tor used in clone().
         */
        protected LiteralExpr(LiteralExpr other) : base(other)
        {
        }

        /**
         * Returns an analyzed literal of 'type'. Returns null for types that do not have a
         * LiteralExpr subclass, e.g. TIMESTAMP.
         */
        public static LiteralExpr create(String value, SqlNodeType type)
        {
            //Preconditions.checkArgument(type.isValid());
            LiteralExpr e = null;
            switch (type.getPrimitiveType())
            {
                case NULL_TYPE:
                    e = new NullLiteral();
                    break;
                case BOOLEAN:
                    e = new BoolLiteral(value);
                    break;
                case TINYINT:
                case SMALLINT:
                case INT:
                case BIGINT:
                case FLOAT:
                case DOUBLE:
                case DECIMAL:
                    e = new NumericLiteral(value, type);
                    break;
                case STRING:
                case VARCHAR:
                case CHAR:
                    e = new StringLiteral(value);
                    break;
                case DATE:
                case DATETIME:
                case TIMESTAMP:
                    // TODO: we support TIMESTAMP but no way to specify it in SQL.
                    return null;
                //default:
                    //Preconditions.checkState(false,
                    //    String.format("Literals of type '%s' not supported.", type.toSql()));
            }

            e.analyze(null);
            // Need to cast since we cannot infer the type from the value. e.g. value
            // can be parsed as tinyint but we need a bigint.
            return (LiteralExpr) e.uncheckedCastTo(type);
        }

        //@Override
        protected void analyzeImpl(Analyzer analyzer)
        {
            // Literals require no analysis.
        }

        //@Override
        protected float computeEvalCost()
        {
            return LITERAL_COST;
        }

        /**
         * Returns an analyzed literal from the thrift object.
         */
        //public static LiteralExpr fromThrift(TExprNode exprNode, SqlNodeType colType)
        //{
        //    try
        //    {
        //        LiteralExpr result = null;
        //        switch (exprNode.node_type)
        //        {
        //            case FLOAT_LITERAL:
        //                result = LiteralExpr.create(
        //                    Double.toString(exprNode.float_literal.value), colType);
        //                break;
        //            case DECIMAL_LITERAL:
        //                byte[] bytes = exprNode.decimal_literal.getValue();
        //                decimal val = new decimal(new BigInteger(bytes));
        //                ScalarType decimalType = (ScalarType)colType;
        //                // We store the decimal as the unscaled bytes. Need to adjust for the scale.
        //                val = val.movePointLeft(decimalType.decimalScale());
        //                result = new NumericLiteral(val, colType);
        //                break;
        //            case INT_LITERAL:
        //                result = LiteralExpr.create(
        //                    Long.toString(exprNode.int_literal.value), colType);
        //                break;
        //            case STRING_LITERAL:
        //                result = LiteralExpr.create(exprNode.string_literal.value, colType);
        //                break;
        //            case BOOL_LITERAL:
        //                result = LiteralExpr.create(
        //                    Boolean.toString(exprNode.bool_literal.value), colType);
        //                break;
        //            case NULL_LITERAL:
        //                return NullLiteral.create(colType);
        //            default:
        //                throw new UnsupportedOperationException("Unsupported partition key type: " +
        //                    exprNode.node_type);
        //        }
        //        Preconditions.checkNotNull(result);
        //        result.analyze(null);
        //        return result;
        //    }
        //    catch (Exception e)
        //    {
        //        throw new IllegalStateException("Error creating LiteralExpr: ", e);
        //    }
        //}

        // Returns the string representation of the literal's value. Used when passing
        // literal values to the metastore rather than to Impala backends. This is similar to
        // the toSql() method, but does not perform any formatting of the string values. Neither
        // method unescapes string values.
        //public String getStringValue();

        // Swaps the sign of numeric literals.
        // Throws for non-numeric literals.
        public void swapSign()
        {
            throw new NotImplementedException("swapSign() only implemented for numeric" +
                                              "literals");
        }

        /**
         * Evaluates the given constant expr and returns its result as a LiteralExpr.
         * Assumes expr has been analyzed. Returns constExpr if is it already a LiteralExpr.
         * Returns null for types that do not have a LiteralExpr subclass, e.g. TIMESTAMP, or
         * in cases where the corresponding LiteralExpr is not able to represent the evaluation
         * result, e.g., NaN or infinity. Returns null if the expr evaluation encountered errors
         * or warnings in the BE.
         * TODO: Support non-scalar types.
         */
        public static LiteralExpr create(Expr constExpr, TQueryCtx queryCtx)
        {
            //Preconditions.checkState(constExpr.isConstant());
            //Preconditions.checkState(constExpr.getType().isValid());
            if (constExpr is LiteralExpr) return (LiteralExpr) constExpr;

            TColumnValue val = null;
            try
            {
                val = FeSupport.EvalExprWithoutRow(constExpr, queryCtx);
            }
            catch (InternalException e)
            {
                LOG.error(String.format("Failed to evaluate expr '%s': %s",
                    constExpr.toSql(), e.getMessage()));
                return null;
            }

            LiteralExpr result = null;
            switch (constExpr.getType().getPrimitiveType())
            {
                case NULL_TYPE:
                    result = new NullLiteral();
                    break;
                case BOOLEAN:
                    if (val.isSetBool_val()) result = new BoolLiteral(val.bool_val);
                    break;
                case TINYINT:
                    if (val.isSetByte_val())
                    {
                        result = new NumericLiteral(decimal.valueOf(val.byte_val));
                    }

                    break;
                case SMALLINT:
                    if (val.isSetShort_val())
                    {
                        result = new NumericLiteral(decimal.valueOf(val.short_val));
                    }

                    break;
                case INT:
                    if (val.isSetInt_val())
                    {
                        result = new NumericLiteral(decimal.valueOf(val.int_val));
                    }

                    break;
                case BIGINT:
                    if (val.isSetLong_val())
                    {
                        result = new NumericLiteral(decimal.valueOf(val.long_val));
                    }

                    break;
                case FLOAT:
                case DOUBLE:
                    if (val.isSetDouble_val())
                    {
                        // A NumericLiteral cannot represent NaN, infinity or negative zero.
                        if (!NumericLiteral.isValidLiteral(val.double_val)) return null;
                        result = new NumericLiteral(new decimal(val.double_val), constExpr.getType());
                    }

                    break;
                case DECIMAL:
                    if (val.isSetString_val())
                    {
                        result =
                            new NumericLiteral(new decimal(val.string_val), constExpr.getType());
                    }

                    break;
                case STRING:
                case VARCHAR:
                case CHAR:
                    if (val.isSetBinary_val())
                    {
                        byte[] bytes = new byte[val.binary_val.remaining()];
                        val.binary_val.get(bytes);
                        // Converting strings between the BE/FE does not work properly for the
                        // extended ASCII characters above 127. Bail in such cases to avoid
                        // producing incorrect results.
                        if (bytes.Any(b => b < 0))
                        {
                            return null;
                        }
                        try
                        {
                            // US-ASCII is 7-bit ASCII.
                            String strVal = new String(bytes, "US-ASCII");
                            // The evaluation result is a raw string that must not be unescaped.
                            result = new StringLiteral(strVal, constExpr.getType(), false);
                        }
                        catch (UnsupportedEncodingException e)
                        {
                            return null;
                        }
                    }

                    break;
                case TIMESTAMP:
                    // Expects both the binary and string fields to be set, so we get the raw
                    // representation as well as the string representation.
                    if (val.isSetBinary_val() && val.isSetString_val())
                    {
                        result = new TimestampLiteral(val.getBinary_val(), val.getString_val());
                    }

                    break;
                case DATE:
                case DATETIME:
                    return null;
                //default:
                //Preconditions.checkState(false,
                //    String.format("Literals of type '%s' not supported.",
                //        constExpr.getType().toSql()));
            }

            // None of the fields in the thrift struct were set indicating a NULL.
            if (result == null) result = new NullLiteral();

            result.analyzeNoThrow(null);
            return (LiteralExpr) result;
        }

// Order NullLiterals based on the SQL ORDER BY default behavior: NULLS LAST.
        public int compareTo(LiteralExpr other)
        {
            if (this is NullLiteral && other is NullLiteral) return 0;
            if (this is NullLiteral) return -1;
            if (other is NullLiteral) return 1;
            if (getClass() != other.getClass()) return -1;
            return 0;
        }
    }
}