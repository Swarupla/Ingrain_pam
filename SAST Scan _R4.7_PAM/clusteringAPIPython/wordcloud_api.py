from wordcloud import WordCloud, STOPWORDS 
import matplotlib.pyplot as plt 
import pandas as pd 
import base64
import io
from SSAIutils import utils
import regex as re
from datapreprocessing import data_quality_check_Clustering
from nltk.corpus import stopwords
default_stopwords = stopwords.words('english')
#print(default_stopwords)

def gen_wordcloud(correlationId,stopword_list,pageInfo,max_words):
    response={}
    try:
        data_t = utils.data_from_chunks(corid=correlationId, collection="Clustering_BusinessProblem")
        data_json=utils.check_columns(corid=correlationId, collection="Clustering_IngestData")
        #print(data_json[0].get('Columnsselectedbyuser'))
        columns_list=data_json[0].get('Columnsselectedbyuser')
        #print("Fetching data-----------",data_t)
    except Exception as e:
        print("Not able to fetch data")
        print(e)
    
    # Reads 'Youtube04-Eminem.csv' file 
    df=data_t
    #df = pd.read_csv(r"cr2.csv") 
    #print(df.head())
    #df= df.notna()
    comment_words = '' 
    stopwords = set(STOPWORDS)
    print("stopwords list----",stopword_list) 
    #columns_list=['age','id','sex']
    print("Columns_list----",columns_list)
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
  
    wordcloud=WordCloud(width = 720, height = 720, background_color ='white',prefer_horizontal=1, stopwords = stopwords, min_font_size = 10,max_words=max_words,collocations=False)
    #print("no of words-----",len(word_list))
    
    word_json=WordCloud().process_text(comment_words)
    #print(word_json)
    print(len(word_json))
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
    #Saving image
    #wordcloud.to_file(str(max_words)+".png")

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
    print(response["message"])       
    return response
    #return img_b64

    # plot the WordCloud image                        
    #plt.figure(figsize = (8, 8), facecolor = None) 
    #plt.imshow(wordcloud) 
    #plt.axis("off") 
    #plt.tight_layout(pad = 0) 
  
    #plt.show() 

    #https://stackoverflow.com/questions/49537474/wordcloud-of-bigram-using-python