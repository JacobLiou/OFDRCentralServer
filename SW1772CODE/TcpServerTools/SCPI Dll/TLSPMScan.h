typedef struct tagAutoTestParam
{                                 
	BOOL	m_bPDL;
	int m_nChannelCount;
	int m_nArrayCapacity;
	int  m_nRange[64];
	double	m_pdblWavelengthArray[10240];  
	double	m_pdblLossArrayP1[64][10240]; 
	double	m_pdblLossArrayP2[64][10240]; 
	double	m_pdblLossArrayP3[64][10240]; 
	double	m_pdblLossArrayP4[64][10240]; 
} stAutoTestParam, *PAutoTestParam;
BOOL OpenDevice(BOOL bAlpha=FALSE);  //bAlpha=TRUE do alpha scan;
BOOL AutoTest(PAutoTestParam pAutoTestData);