// SpecScan_User.h - Contains declaration of Function class  
// Library version: 10.03.0S 12/23/2020

#define SPECSCANLIB_API __declspec(dllimport)
 
namespace GMT_SpecScan
{  	
SPECSCANLIB_API void WINAPI SpecScan_Initialize_Lib(BOOL ShowErrorMsg);
SPECSCANLIB_API void WINAPI SpecScan_Get_Lib_Version(char* Lib_Ver);
SPECSCANLIB_API int WINAPI SpecScan_Initialize_Port(int PortID);
SPECSCANLIB_API int WINAPI SpecScan_Initialize_PSG_Port(int PortID);
SPECSCANLIB_API int WINAPI SpecScan_Initialize_Connection(int Handle);
SPECSCANLIB_API int WINAPI SpecScan_Get_Info(int Handle, char* Device_Info);
SPECSCANLIB_API int WINAPI SpecScan_Initialize_N7745A_Gpib(int *Gpib_Address);
SPECSCANLIB_API int WINAPI SpecScan_Load_FreqClock_Data(int Handle, char* Fclk_Data_File);

SPECSCANLIB_API int WINAPI SpecScan_Is_Scan_Done(int Handle);

SPECSCANLIB_API int WINAPI SpecScan_Start_Scan(int Handle, int StartWL, int StopWL, int SweepSpeed, int AveNum, int DataLength, int Ch_ID, BOOL SyncSweep);
SPECSCANLIB_API int WINAPI SpecScan_Get_ScanData(int Handle, float* WL_Data, float* SP_Data, int Length, int Ch_ID);
SPECSCANLIB_API int WINAPI SpecScan_AlignMode_Scan(int Handle, int StartWL, int StopWL, int SweepSpeed, int Ch_ID, BOOL SyncSweep);
SPECSCANLIB_API int WINAPI SpecScan_Get_AlignMode_Data(int Handle, float* WL_Data, float* SP_Data, int* DataLength, int Ch_ID);
SPECSCANLIB_API int WINAPI SpecScan_Start_PSG(int Handle);
SPECSCANLIB_API int WINAPI SpecScan_Stop_PSG(int Handle);
SPECSCANLIB_API int WINAPI SpecScan_Get_PSG_Temp(int Handle, float* Temp, int* PSG_Run);
SPECSCANLIB_API int WINAPI SpecScan_Load_PSG_Data(char* PSG_Data_File);
SPECSCANLIB_API int WINAPI SpecScan_Start_PDL_Scan(int Handle, int StartWL, int StopWL, int SweepSpeed, int AveNum, int DataLength, int Ch_ID, BOOL SyncSweep, float PSG_Temp);
SPECSCANLIB_API int WINAPI SpecScan_Get_PDL_Data(int Handle, float* WL_Data, float* PDL_Data, int Length, int Ch_ID);
SPECSCANLIB_API int WINAPI SpecScan_Get_PDL_WF(int Handle, float* WL_Data, float* SP_Data, int Length, int Ch_ID);

SPECSCANLIB_API int WINAPI SpecScan_Read_PowerMeter(int Handle, int Ch_ID, float* PowerData);

SPECSCANLIB_API int WINAPI SpecScan_Start_REF_Scan(int Handle, int StartWL, int StopWL, int SweepSpeed, int* DataLength, BOOL SyncSweep);
SPECSCANLIB_API int WINAPI SpecScan_Get_REF_ScanData(int Handle, float* WL_Data, float* SP_Data, int* Length);

SPECSCANLIB_API int WINAPI SpecScan_Initialize_N7745A_Scan(int *Gpib_Address, int* Max_Power);
SPECSCANLIB_API int WINAPI SpecScan_Set_N7745A_Ref_Ch(int Gpib_Address, int Ref_Ch);
SPECSCANLIB_API void WINAPI SpecScan_Get_N7745A_Ref_Ch(int* Gpib_Address, int* Ref_Ch);
SPECSCANLIB_API int WINAPI SpecScan_Start_Scan_N7745A(int Handle, int StartWL, int StopWL, int SweepSpeed, int DataLength, int DataType);
SPECSCANLIB_API int WINAPI SpecScan_Get_ScanData_N7745A(int Handle, int Gpib_Address, float* WL_Data,  float* SP_Data, int Length, int Ch_ID, int DataType);

SPECSCANLIB_API int WINAPI SpecScan_Set_Clock_Divider(int Handle, int N_divider);
SPECSCANLIB_API int WINAPI SpecScan_Set_LSA200N_to_LSA200A(int Handle);

SPECSCANLIB_API void WINAPI SpecScan_Stop_All();
SPECSCANLIB_API int WINAPI SpecScan_Stop_Scan(int Handle);
SPECSCANLIB_API void WINAPI SpecScan_Release_All();
SPECSCANLIB_API int WINAPI SpecScan_Release(int Handle);
SPECSCANLIB_API void WINAPI SpecScan_Release_N7745A_Gpib_All();
SPECSCANLIB_API int WINAPI SpecScan_Release_N7745A_Gpib(int Gpib_Addr);



//SPECSCANLIB_API void WINAPI SpecScan_Initialize_Lib(BOOL ShowErrorMsg);
//SPECSCANLIB_API void WINAPI SpecScan_Get_Lib_Version(char* Lib_Ver);
//SPECSCANLIB_API int WINAPI SpecScan_Initialize_Port(int PortID);
//SPECSCANLIB_API int WINAPI SpecScan_Initialize_Pol_Port(int PortID);
//SPECSCANLIB_API int WINAPI SpecScan_Initialize_Connection(int Handle);
//SPECSCANLIB_API int WINAPI SpecScan_Get_Info(int Handle, char* Device_Info);
//SPECSCANLIB_API int WINAPI SpecScan_Load_FreqClock_Data(int Handle, char* Fclk_Data_File);
//SPECSCANLIB_API int WINAPI SpecScan_Read_PowerMeter(int Handle, int Ch_ID, float* PowerData);
//SPECSCANLIB_API int WINAPI SpecScan_AlignMode_Scan(int Handle, int StartWL, int StopWL, int SweepSpeed, int Ch_ID, BOOL SyncSweep);
//SPECSCANLIB_API int WINAPI SpecScan_Get_AlignMode_Data(int Handle, float* WL_Data, float* SP_Data, int* DataLength, int Ch_ID);
//SPECSCANLIB_API int WINAPI SpecScan_Start_Scan(int Handle, int StartWL, int StopWL, int SweepSpeed, int AveNum, int DataLength, int Ch_ID, BOOL SyncSweep);
//SPECSCANLIB_API int WINAPI SpecScan_Get_ScanData(int Handle, float* WL_Data, float* SP_Data, int Length, int Ch_ID);
//SPECSCANLIB_API int WINAPI SpecScan_Start_PDL_Scan(int Handle, int StartWL, int StopWL, int SweepSpeed, int AveNum, int DataLength, int Ch_ID, BOOL SyncSweep);
//SPECSCANLIB_API int WINAPI SpecScan_Get_PDL_Data(int Handle, float* WL_Data, float* PDL_Data, int Length, int Ch_ID);
//SPECSCANLIB_API int WINAPI SpecScan_Get_PDL_WF(int Handle, float* WL_Data, float* SP_Data, int Length, int Ch_ID);
//SPECSCANLIB_API int WINAPI SpecScan_Start_REF_Scan(int Handle, int StartWL, int StopWL, int SweepSpeed, int* DataLength, BOOL SyncSweep);
//SPECSCANLIB_API int WINAPI SpecScan_Get_REF_ScanData(int Handle, float* WL_Data, float* SP_Data, int* Length);
//SPECSCANLIB_API int WINAPI SpecScan_Is_Scan_Done(int Handle);
//SPECSCANLIB_API void WINAPI SpecScan_Stop_All();
//SPECSCANLIB_API int WINAPI SpecScan_Stop_Scan(int Handle);
//SPECSCANLIB_API void WINAPI SpecScan_Release_All();
//SPECSCANLIB_API int WINAPI SpecScan_Release(int Handle);



}  

////////////////////////////////////////////////////////////////////////////////////