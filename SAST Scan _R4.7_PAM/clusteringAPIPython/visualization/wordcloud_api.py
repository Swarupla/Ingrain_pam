from wordcloud import WordCloud, STOPWORDS 
#import matplotlib.pyplot as plt 
import pandas as pd 
import base64
import io
from SSAIutils import utils
import regex as re
#from datapreprocessing import data_quality_check_Clustering
from nltk.corpus import stopwords
default_stopwords = stopwords.words('english')
#print(default_stopwords)

def gen_wordcloud(correlationId,stopword_list,pageInfo,max_words,flag=None,df=None):
    response={}
    if flag == None or flag==False:
        flag=False
    else:
        flag=True
    
    if flag!=True:
    
        try:
            offlineutility = utils.checkofflineutility(correlationId)
            if offlineutility:
                data_t = utils.data_from_chunks_offline_utility(corid=correlationId, collection="DataSet_IngestData",lime=None, recent=None,DataSetUId=offlineutility)
            else:
                data_t = utils.data_from_chunks(corid=correlationId,collection="Clustering_BusinessProblem")  

            data_json=utils.check_columns(corid=correlationId, collection="Clustering_IngestData")
        #print(data_json[0].get('Columnsselectedbyuser'))
            columns_list=data_json[0].get('Columnsselectedbyuser')
            df=data_t
        #print("Fetching data-----------",data_t)
        except Exception as e:
            raise Exception("Not able to fetch data")
            
    elif flag==True:
        df=df
        columns_list=None
        
    comment_words = '' 
    stopwords = set(STOPWORDS)
     
    #columns_list=['age','id','sex']
    
    stopwords.update(default_stopwords)
    new_def=['youre', 'youve', 'youll', 'youd', 'shes', 'its', 'thatll', 'dont', 'shouldve', 'arent', 'couldnt', 'didnt', 'doesnt', 'hadnt', 'hasnt', 'havent', 'isnt', 'mightnt', 'mustnt', 'neednt', 'shant', 'shouldnt', 'wasnt', 'werent', 'wont', 'wouldnt']
    stopwords.update(new_def)
    
    if len(stopword_list)<1:
        stopwords=stopwords
    else:
        stopwords.update(stopword_list)
        #for i in range(1,len(stopword_list)):
        #stopwords.update(stopword_list[i])
    if columns_list!=None:
        df=df[columns_list]

    stopwords.update(["nan","none","a","b","c","d","e","f","g","h","i","j","k","l","m","n","o","p","q","r","s","t","u","v","w","x","y","z"])
    #print("Data-----",df)
    #word_list=[]
  
    # iterate through the complete csv file 
    for col,row in enumerate(df.values):
        for i in range(len(row)):
            val=row[i]
            #if type(val)==type("abcd"):
            # typecaste each val to string 
            val = str(val)
            pattern = r'[^a-zA-Z\s]'    
            val=re.sub(pattern, '', val)
            #print(val)
            # split the value 
            tokens = val.split() 
            # Converts each token into lowercase 
            for i in range(len(tokens)): 
                tokens[i] = tokens[i].lower() 
            comment_words += " ".join(tokens)+" "
            #print(comment_words)
            #print((tokens))
            #if(type(tokens)==type("abcd")):
            #print(tokens)
            #word_list.append(tokens)
  
    wordcloud=WordCloud(width = 1230, height = 1230, background_color ='white', stopwords = stopwords, min_font_size = 10,max_words=max_words,collocations=False)
    #print("no of words-----",len(word_list))
    
    word_json=WordCloud().process_text(comment_words)
    #print(word_json)
    
    #print(len(y))
    try:
        if len(word_json) >12:
            wordcloud=wordcloud.generate(comment_words)
            
        else:
            response["output"]=""
            response["message"]="Choose text columns with atleast 10 words for the wordcloud to have a plot."
            return response
        
    except Exception as e:
        #response[]
        response["output"]=""
        response["message"]="Choose text columns with atleast 10 words for the wordcloud to have a plot."
        return response

    #Convert image to base 64 string
    img = io.BytesIO()
    pil_img=wordcloud.to_image()
    pil_img.save(img, "PNG")
    img.seek(0)
    img_b64 = base64.b64encode(img.getvalue())#.decode()
    response["output"]=str(img_b64)
    response["message"]="Success"
    #img_b64=img_b64.encode('UTF-8')#.decode('UTF-8')
    #print(img_b64)
    #Clustering_StatusTable
    #utils.updQdb(correlationId,'C',"100",pageInfo,userId,userdefinedmodelname=img_b64)   
           
    return response