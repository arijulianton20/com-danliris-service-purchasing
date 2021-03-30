using System;
using System.Collections.Generic;
using System.Text;

namespace Com.DanLiris.Service.Purchasing.Lib.Services.GarmentDebtBalance
{
    public class InternalNoteFormDto
    {
        public InternalNoteFormDto()
        {

        }

        public InternalNoteFormDto(int internalNoteId, string internalNoteNo)
        {
            InternalNoteId = internalNoteId;
            InternalNoteNo = internalNoteNo;
        }

        public int InternalNoteId { get; private set; }
        public string InternalNoteNo { get; private set; }
    }
}
