import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning) 
import sys
if not sys.warnoptions:
    warnings.simplefilter("ignore")
from SSAIutils import utils

def get_OGData(correlationId):
    data = utils.data_from_chunks(correlationId,'DE_PreProcessedData')
    '''CHANGES START HERE'''
    dbproconn,dbprocollection = utils.open_dbconn("DE_AddNewFeature")
    data_json = dbprocollection.find({"CorrelationId" :correlationId}) 
#    feature_not_created = data_json[0].get("Feature_Not_Created")
    try: 
        features_created          = data_json[0].get("Features_Created")
        encoded_new_feature = data_json[0].get("Map_Encode_New_Feature")
    except Exception:
        features_created = []
        encoded_new_feature = {} 
    
    '''CHANGES END HERE'''
    encoders = {}
    
    # Fetch data to be encoded from data processing table
    dbproconn,dbprocollection = utils.open_dbconn("DE_DataProcessing")
    data_json = dbprocollection.find({"CorrelationId" :correlationId}) 
    dbproconn.close()
    
    Data_to_Encode=data_json[0].get('DataEncoding')
    '''CHANGES START HERE'''
    map_encode_new_feature = {}
    if len(encoded_new_feature)>0:
        for i in range(len(encoded_new_feature)):
            map_encode_new_feature[encoded_new_feature[i]] = {'attribute': 'Nominal','encoding': 'Label Encoding','ChangeRequest': 'True','PChangeRequest': 'False'}
    Data_to_Encode.update(map_encode_new_feature)
    
    '''CHANGES END HERE'''
    if len(Data_to_Encode) > 0:
        OHEcols = []
        LEcols = []
        
        for keys,values in Data_to_Encode.items():                
            if values.get('encoding') == 'One Hot Encoding' and (values.get('ChangeRequest') == 'True' and values.get('PChangeRequest') != 'True') and (keys != 'ChangeRequest' and keys!='PChangeRequest'):
                OHEcols.append(keys)
            elif values.get('encoding') == 'Label Encoding' and (values.get('ChangeRequest') == 'True' and values.get('PChangeRequest') != 'True') and (keys != 'ChangeRequest' and keys!='PChangeRequest'):
                LEcols.append(keys)
        #OHE
        # Fetch Pickle file and encoded columns
        if len(OHEcols)>0:
            ohem,_,enc_cols,_ = utils.get_pickle_file(correlationId,FileType='OHE')
            encoders={'OHE':{ohem:{'EncCols':enc_cols,'OGCols':OHEcols}}}
            
        if len(LEcols)>0:
            lencm,_,Lenc_cols,_ = utils.get_pickle_file(correlationId,FileType='LE')
            for nf in features_created:
                if (nf in Data_to_Encode) and (nf+"_L" not in Lenc_cols):
                    Lenc_cols.append(nf+"_L")
            encoders.update({'LE':{lencm:Lenc_cols}})    
                        
        OGData = utils.get_OGDataFrame(data,encoders)
    else:
        OGData = data
    return OGData

def new_features_data(features_created,OGData):
    new_features_data = {}
    for i in features_created:
        new_features_data[i]=list(OGData[i])
    return new_features_data