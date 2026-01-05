using System;
using System.Collections.Generic;
using System.Text;
using NispQuery;
using System.Data;
using System.Data.OleDb;

namespace ProReksa2
{
    public class mKonfirmasi
    {
        private ClsQuery _cQuery;
        private string _refID;
        private DataSet _ds;

        #region Constructor
        public mKonfirmasi(ClsQuery cQuery)
        {
            _cQuery = cQuery;
        }
        #endregion

        #region Method
        public bool InquirySubscription()
        {
            OleDbParameter[] dbParams = new OleDbParameter[1];
            dbParams[0] = new OleDbParameter("@pcRefID", OleDbType.VarChar);
            dbParams[0].Value = _refID;

            return _cQuery.ExecProc("ReksaKonfirmasiSubscription", ref dbParams, out _ds);
        }

        public bool InquiryRedemption()
        {
            OleDbParameter[] dbParams = new OleDbParameter[1];
            dbParams[0] = new OleDbParameter("@pcRefID", OleDbType.VarChar);
            dbParams[0].Value = _refID;

            return _cQuery.ExecProc("ReksaKonfirmasiRedemption", ref dbParams, out _ds);
        }

        public bool InquiryRDB()
        {
            OleDbParameter[] dbParams = new OleDbParameter[1];
            dbParams[0] = new OleDbParameter("@pcRefID", OleDbType.VarChar);
            dbParams[0].Value = _refID;

            return _cQuery.ExecProc("ReksaKonfirmasiRDB", ref dbParams, out _ds);
        }

        public bool InquirySwitching()
        {
            OleDbParameter[] dbParams = new OleDbParameter[1];
            dbParams[0] = new OleDbParameter("@pcRefID", OleDbType.VarChar);
            dbParams[0].Value = _refID;

            return _cQuery.ExecProc("ReksaKonfirmasiSwitching", ref dbParams, out _ds);
        }

        public bool InquirySwitchingRDB()
        {
            OleDbParameter[] dbParams = new OleDbParameter[1];
            dbParams[0] = new OleDbParameter("@pcRefID", OleDbType.VarChar);
            dbParams[0].Value = _refID;

            return _cQuery.ExecProc("ReksaKonfirmasiSwitchingRDB", ref dbParams, out _ds);
        }

        public bool InquiryBooking()
        {
            OleDbParameter[] dbParams = new OleDbParameter[1];
            dbParams[0] = new OleDbParameter("@pcRefID", OleDbType.VarChar);
            dbParams[0].Value = _refID;

            return _cQuery.ExecProc("ReksaKonfirmasiBooking", ref dbParams, out _ds);
        }
        #endregion

        #region Property
        public string RefID
        {
            set { _refID = value; }
        }

        public DataSet Ds
        {
            get { return _ds; }
        }
        #endregion
    }
}
