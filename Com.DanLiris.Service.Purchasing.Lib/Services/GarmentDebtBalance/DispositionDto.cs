using System.Collections.Generic;

namespace Com.DanLiris.Service.Purchasing.Lib.Services.GarmentDebtBalance
{
    public class DispositionDto
    {
        public DispositionDto(int dispositionId, string dispositionNo, List<MemoDetail> memoDetails)
        {
            DispositionId = dispositionId;
            DispositionNo = dispositionNo;
            MemoDetails = memoDetails;
        }

        public int DispositionId { get; private set; }
        public string DispositionNo { get; private set; }
        public List<MemoDetail> MemoDetails { get; private set; }
    }
}