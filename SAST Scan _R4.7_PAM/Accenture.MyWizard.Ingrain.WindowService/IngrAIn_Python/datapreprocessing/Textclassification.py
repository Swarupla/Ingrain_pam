# -*- coding: utf-8 -*-
"""
Created on Fri Dec 13 10:58:40 2019

@author: ravi.kiran.sirigiri
"""
import sys
import numpy as np
#sys.modules['numpy.random.bit_generator']=np.random._bit_generator
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning)
if not sys.warnoptions:
    warnings.simplefilter("ignore")
import pandas as pd
# Laoding libraries - ML related
from sklearn.feature_extraction.text import CountVectorizer
from sklearn.feature_extraction.text import TfidfVectorizer
import platform
import multiprocessing 
from joblib import parallel_backend
from joblib import Parallel, delayed

from sklearn import metrics

#os.chdir("C:\\Users\\ravi.kiran.sirigiri\\Documents\\Ingrain_NLP") #setting working directory
# Laoding libraries -Text Processing
from nltk.tag import pos_tag
from nltk.stem import PorterStemmer, WordNetLemmatizer
from nltk.stem.snowball import SnowballStemmer
import nltk
import re, string
from nltk.corpus import wordnet as wn
from nltk.corpus import stopwords
import string
# Laoding libraries -Utility
from functools import lru_cache
from wordcloud import WordCloud
from SSAIutils import utils
import tempfile
import base64
from gensim.models import Word2Vec
import spacy
from sklearn.cluster import MiniBatchKMeans
import matplotlib.pyplot as plt

import base64
import json
from SSAIutils import EncryptData
import fasttext,fasttext.util

# =============================================================================
# Text Processing
# =============================================================================
#Defining lematizer, pos tagger and stemmer
wnl     = WordNetLemmatizer()
tagger  = pos_tag
porter  = PorterStemmer()


#memonization - to increase speed n efficiency
lemmatize_mem = lru_cache(maxsize=16384)(wnl.lemmatize)
tagger_mem = lru_cache(maxsize=16384)(tagger)

# ***Defining Fucntions***
#convert treebank tags to wordnet tags

if platform.system() == 'Linux':
    thai_model_path='fasttext_model/cc.th.100.bin'
elif platform.system() == 'Windows':
    thai_model_path='fasttext_model\\cc.th.100.bin'
   
class SimpleGroupedColorFunc(object):
    """Create a color function object which assigns EXACT colors
       to certain words based on the color to words mapping


       Parameters
       ----------
       color_to_words : dict(str -> list(str))
         A dictionary that maps a color to the list of words.


       default_color : str
         Color that will be assigned to a word that's not a member
         of any value from color_to_words.
    """

    def __init__(self, color_to_words, default_color):
        self.word_to_color = {word: color
                              for (color, words) in color_to_words.items()
                              for word in words}


        self.default_color = default_color


    def __call__(self, word, **kwargs):
        return self.word_to_color.get(word, self.default_color)


def tree_2_word(treebank_tag):
    if treebank_tag.startswith('J'):
        return wn.ADJ
    elif treebank_tag.startswith('V'):
        return wn.VERB
    elif treebank_tag.startswith('R'):
        return wn.ADV
    else:
        return wn.NOUN
    
def lemmatizer(word,pos=wn.NOUN):
    return(lemmatize_mem(word,pos))
     
def text_process(t,pos=True,lemmetize=True,stem=True,stop_words=[]):
    if stop_words!=[]:
        stop_words=[str(i).lower() for i in stop_words]
        stops = set(list(stopwords.words("english"))+(stop_words))
    else:
        stops = set(stopwords.words("english"))       # Defining stop words
    t = re.sub('[\n\t\r]+',' ',t)                         # Remove linebreak, tab, return
    t = t.lower()                                         # Convert to lower case
    t = re.sub('[^a-zA-Z]',' ',t)                         # Remove Non-letters
    #sentence = nltk.sent_tokenize(t)                      # Sentence wise tokenize
    #sentence=t
    modified_sentence=""
    #for s in sentence:
    words = nltk.word_tokenize(t)                     # Word Tokenization 
    if pos:                                           # Part of speech Tagging
        tag_words = pos_tag(words)
        #print(tag_words)

        lemmatized= ""
        for tw in tag_words:
            if tw[0] not in stops:
                if lemmetize:                         # lemmatization
                    #print(tree_2_word(tw[1]))
                    lemma=lemmatizer(tw[0],tree_2_word(tw[1])) 
                    #print(lemma)
                    #if (stem) & (not wn.synsets(lemma)):
                        #lemma = porter.stem(tw[0])    # Stemming

                if stem:
                    if lemmetize:
                         lemma=porter.stem(lemma)
                    else:
                        lemma=porter.stem(tw[0])
                if not lemmetize and not stem:
                    lemma=tw[0]
                #print(lemmatized)
                lemmatized=lemmatized+" "+lemma
    else:
        lemmatized= ""
        for w in words:
            if w not in stops:
                if lemmetize:                         # lemmatization
                    lemma=lemmatizer(w)
                    #if (stem) & (not wn.synsets(lemma)):
                        #lemma = porter.stem(w)   
                if stem:
                    if lemmetize:
                        lemma=porter.stem(lemma)
                    else:                        
                        lemma=porter.stem(w)
                if not lemmetize and not stem:
                    lemma=w

                lemmatized=lemmatized+" "+lemma               
    modified_sentence=modified_sentence+" "+lemmatized
    modified_sentence=modified_sentence.strip()
    t = re.sub('['+string.punctuation+']+','',\
               modified_sentence)                          # Remove Punctuations     
    t = re.sub('\s+\s+',' ',t)                             # Remove double whitespace
    return(t)

def text_process1(t,pos=True,lemmetize=True,stem=True,stop_words=[],language ='english',spacy_vectors= spacy.load('en_core_web_lg')):
    if language!="thai":
        #print(t)
        if stop_words!=[]:
            stop_words=[str(i).lower() for i in stop_words]
            stops = set(list(stopwords.words(language))+(stop_words))
            
        else:
            # Defining stop words
            stops = set(stopwords.words(language))
            
        t = re.sub('[\n\t\r]+',' ',t)                         # Remove linebreak, tab, return
        t = t.lower()                                         # Convert to lower case
        if language =='english':
            t = re.sub('[^a-zA-Z]',' ',t)                         # Remove Non-letters
        else:
            table = str.maketrans(dict.fromkeys(string.punctuation))
            t= t.translate(table)
        #sentence = nltk.sent_tokenize(t)                      # Sentence wise tokenize
        #sentence=t
        modified_sentence=""
        #for s in sentence:
        words = nltk.word_tokenize(t)                     # Word Tokenization 
        #nlp = spacy.load("en_core_web_sm")
        
        lemmatized= ""
        doc = spacy_vectors(t)
        for token in doc:
            if str(token) not in stops:
                if lemmetize:                         # lemmatization
                    #print(tree_2_word(tw[1]))
                        lemma=token.lemma_
                        #print(lemma)
                    #if (stem) & (not wn.synsets(lemma)):
                        #lemma = porter.stem(w)   
                if stem:
                    stemmer = SnowballStemmer(language)
                    if lemmetize:
                        lemma=stemmer.stem(lemma)
                    else:                        
                        lemma=stemmer.stem(str(token))
                if not lemmetize and not stem:
                    lemma=str(token)
    
                lemmatized=lemmatized+" "+lemma               
        modified_sentence=modified_sentence+" "+lemmatized
        modified_sentence=modified_sentence.strip()
        t = re.sub('['+string.punctuation+']+','',\
                   modified_sentence)                          # Remove Punctuations     
        t = re.sub('\s+\s+',' ',t)                             # Remove double whitespace
    return(t)
    

def clustering(text,n_cluster,correlationId,language ='english'):
    #if language=='french':
    #    language='english'
    #tfidf_vectorizer = TfidfVectorizer(stop_words="english")
    #tfidf = tfidf_vectorizer.fit_transform(text)
    EnDeRequired = utils.getEncryptionFlag(correlationId)

    dbproconn,dbprocollection = utils.open_dbconn("DE_DataProcessing")
    data_json = list(dbprocollection.find({"CorrelationId" :correlationId}))
    if EnDeRequired :
        t = base64.b64decode(data_json[0].get('DataModification'))
        data_json[0]['DataModification']  =  eval(EncryptData.DescryptIt(t))
    text_dict= data_json[0].get('DataModification',{}).get('TextDataPreprocessing')
    vec=text_dict.get("Feature_Generator")
    n_gram=tuple(text_dict.get("N-grams"))
    
    if vec=='Count Vectorizer':
        vectorizer=CountVectorizer(ngram_range=n_gram)
        X = vectorizer.fit_transform(text) 
        utils.save_vectorizer(correlationId,'Count Vectorization',vectorizer)
    if vec=='Tfidf Vectorizer':
        vectorizer=TfidfVectorizer(ngram_range=n_gram)
        X = vectorizer.fit_transform(text) 
        utils.save_vectorizer(correlationId,'Tfidf Vectorizer',vectorizer)
    if vec=='Word2Vec':
        vectorizer=CountVectorizer(ngram_range=n_gram)
        vectorizer.fit_transform(text)
        allwords=list(vectorizer.get_feature_names())
        model = Word2Vec([allwords],min_count=1)
        X = model[model.wv.vocab].T   
        utils.save_vectorizer(correlationId,'Word2Vec',vectorizer)
    if vec=='Glove':
        vectorizer=CountVectorizer(ngram_range=n_gram)
        vectorizer.fit_transform(text)
        allwords=list(vectorizer.get_feature_names())        
        # Glove implementation using spacy
        #model = spacy.load('en_core_web_lg', disable=['tagger', 'parser'])
        if language =='english':
            spacy_vectors = spacy.load('en_core_web_lg')
        elif language =='spanish':
            spacy_vectors = spacy.load('es_core_news_lg')
        elif language =='portuguese':
            spacy_vectors = spacy.load('pt_core_news_lg')
        elif language =='german':
            spacy_vectors = spacy.load('de_core_news_lg')
        elif language =='chinese':
            spacy_vectors = spacy.load('zh_core_web_lg')
        elif language =='japanese':
            spacy_vectors = spacy.load('ja_core_news_lg')
        elif language =='french':
            spacy_vectors = spacy.load('fr_core_news_lg')
        elif language =='thai':
            spacy_vectors = fasttext.load_model(thai_model_path)
        
        if language !='thai':
            X = np.array([spacy_vectors(word).vector for word in allwords]).T
        else:
            X =  np.array([spacy_vectors[word] for word in allwords]).T
        utils.save_vectorizer(correlationId,'Glove',vectorizer)

    silhouette_graph = None
    if n_cluster==1 or n_cluster==0:
        
        ff=[]
        for i in range(2,15):
            #kmeans=cluster.KMeans(n_clusters=i,random_state=42)
            kmeans = MiniBatchKMeans(n_clusters=i,random_state=42,batch_size=1000,max_iter=300)
            kmeans.fit(X.T)
            labels=kmeans.labels_
            silhouette_score = metrics.silhouette_score(X.T, labels, metric='euclidean',random_state=42)
            ff.append(silhouette_score)
        optimal_clusters=ff.index(max(ff))+2
        #print(optimal_clusters)
        plt.plot(range(2,15),ff)
        plt.xlabel("# of Clusters")
        plt.ylabel("Silhouette Score")
        plt.axvline(x=optimal_clusters, linestyle='--',ymax=0.95)
        #plt.axhline(y=max(ff),linestyle='--',xmax=optimal_clusters/15)
        plt.savefig("silhouette_graph.png")
        with tempfile.TemporaryFile(suffix=".png") as tmpfile:
            tmpfile = open("silhouette_graph.png","rb")
            tmpfile.seek(0)
            silhouette_graph = str(base64.b64encode(tmpfile.read()))
        #silhouette_graph = imageEncoded
    else:
        optimal_clusters=n_cluster
        silhouette_graph = ''
  #################################################################################    
    #kmeans1 = KMeans(optimal_clusters).fit(X.T)
    kmeans1 = MiniBatchKMeans(n_clusters=optimal_clusters,random_state=42,batch_size=1000,max_iter=300).fit(X.T)
    assigned_clusters =kmeans1.labels_
    
    ### Assigning cluster to unique words features
    d={}
    allwords=list(vectorizer.get_feature_names())
    for i in range(len(allwords)):
        # print(d.keys(),assigned_clusters[i])
        if (assigned_clusters[i] in d.keys()):
            d[assigned_clusters[i]].append(allwords[i])
        else:
            d[assigned_clusters[i]]=[allwords[i]]
    
    #### Assigning row wise cluster count ####

 

    # Creating empty dict
#     Cluster_Dict = {'Row_Text': []}
    Cluster_Dict = {}
    for clust in np.unique(assigned_clusters):
        Cluster_Dict['Cluster'+str(clust)] = []

 

    # Assigning count to clusters
    for row_text in text:
#         print(text.index[text == row_text])
#         print(row_text)
#         Cluster_Dict['Row_Text'].append(row_text)
        try:
            vectorizer.fit_transform(pd.Series(row_text))
        except ValueError:
            for k in d.keys():
                Cluster_Dict['Cluster'+str(k)].append(0)
        except:
            print('Error in "Ngram" selection')
        else:
            for key, values in d.items():
                cluster_sum = 0
                for val in vectorizer.get_feature_names():
                    if val in values:
                        cluster_sum += 1
                Cluster_Dict['Cluster'+str(key)].append(cluster_sum)

 

    # Creating Final Cluster Dataframe
    Cluster_DF = pd.DataFrame.from_dict(Cluster_Dict)
    
    utils.store_cluster_dictionary(d,correlationId)
    
    return Cluster_DF, optimal_clusters, silhouette_graph

def wordcloud_weights(row_text, weights, word_dict,vectorizer):
    # ClustertoColor Mapping
    color_names = ["Blue","Red","Green","Black","Gray","Maroon","Yellow","Olive","Lime","Aqua","Teal","Navy","Fuchsia","Silver","Purple"]
    color_codes = ["#0000FF","#FF0000","#008000","#000000","#808080","#800000","#FFFF00","#808000","#00FF00","#00FFFF","#008080","#000080","#FF00FF","#C0C0C0","#800080"]
    
    ClusterToColor = dict(zip(range(15), color_codes))
    NamesToColor = dict(zip(color_names, color_codes))
    d_ColorDict = {ClusterToColor[key] : val for key, val in word_dict.items()}
    

    weights = dict(weights)
    word_weights = {}     # Creating dict for assigning word wise weights
    clust_weights = {}    # Creating dict containing only clusters and weight
    
    vectorizer.fit_transform(pd.Series(row_text))
    row_text_transform = vectorizer.vocabulary_.keys()
# Identifying & mapping word wise initial weights
    for word in row_text_transform:
        for ix in range(len(word_dict.keys())):
            clust_weights[f'Cluster{ix}'] = weights.get(f'Cluster{ix}')   # Creating dict containing only clusters and weights
            if word in word_dict.get(ix):
                w = weights.get(f'Cluster{ix}')
                word_weights[word] = w
    #             print(f'Word: {word} \n Weight: {w} \n Cluster: {ix}')
 

    # Weight modification as per cluster and initial assigned weights
    word_weights_final = {}


    clust_vals = list(clust_weights.values())
    if min(clust_vals)<0:
        for item,value in clust_weights.items():
            clust_weights[item] = value + abs(min(clust_vals))
        for item,value in word_weights.items():
            word_weights[item] = value + abs(min(clust_vals))
    clust_vals = list(clust_weights.values())
    clust_vals.sort()
    clust_max = max(clust_vals)
    for word, weight in word_weights.items():
        if max(clust_weights.values()) > 0:
            word_weights_final[word] = clust_max * (clust_vals.index(weight)+1)
        else:
            word_weights_final[word] = clust_max * (len(clust_vals) - clust_vals.index(weight))

    
    wordcloud = WordCloud(background_color="white", max_words=100).generate_from_frequencies(word_weights_final)
    # Create a color function with multiple tones
    simple_color_func = SimpleGroupedColorFunc(d_ColorDict, default_color='black')
    # Apply our color function
    wordcloud.recolor(color_func=simple_color_func)
    
    with tempfile.TemporaryFile(suffix=".png") as tmpfile:
        
        wordcloud.to_file("word_cloud.png")
        tmpfile = open("word_cloud.png","rb")
        tmpfile.seek(0)
        imageEncoded = str(base64.b64encode(tmpfile.read()))

    return imageEncoded

def clustering_optional(text, correlationId, pageInfo = 'DataPreprocessing',language ='english'):
    '''
    Creates document level word vectors for Count Vectorizer and Tfidf Vectorizer
    Creates document level aggregation of multidimentional word features for Word2Vec and Glove
    '''
    EnDeRequired = utils.getEncryptionFlag(correlationId)
    #if language=='french':
    #    language='english'
    dbproconn,dbprocollection = utils.open_dbconn("DE_DataProcessing")
    data_json = list(dbprocollection.find({"CorrelationId" :correlationId}))
    if EnDeRequired :
        t = base64.b64decode(data_json[0].get('DataModification'))
        data_json[0]['DataModification']  =  eval(EncryptData.DescryptIt(t))
    text_dict= data_json[0].get('DataModification',{}).get('TextDataPreprocessing')
    vec=text_dict.get("Feature_Generator")
    n_gram=tuple(text_dict.get("N-grams"))
    aggregation=text_dict.get("Aggregation")
    if vec=='Count Vectorizer':
        if pageInfo == 'WFTeachTest':
            vectorizer=utils.load_vectorizer(correlationId,'Count Vectorization')
            X = vectorizer.transform(text) 
            vectorized_df = pd.DataFrame(X.toarray()) 
        else:
            vectorizer=CountVectorizer(ngram_range=n_gram)
            X = vectorizer.fit_transform(text) 
            vectorized_df = pd.DataFrame(X.toarray())
            utils.save_vectorizer(correlationId,'Count Vectorization',vectorizer)
    
    if vec=='Tfidf Vectorizer':
        if pageInfo == 'WFTeachTest':
            vectorizer=utils.load_vectorizer(correlationId,'Tfidf Vectorizer')
            X = vectorizer.transform(text) 
            vectorized_df = pd.DataFrame(X.toarray()) 
        else:
            vectorizer=TfidfVectorizer(ngram_range=n_gram)
            X = vectorizer.fit_transform(text) 
            vectorized_df = pd.DataFrame(X.toarray())        
            utils.save_vectorizer(correlationId,'Tfidf Vectorizer',vectorizer)
    
    
    if vec=='Word2Vec' or vec=='Glove':
        
#         glv, w2v  = True, False  # Choose vectorization technique
        #param_doc, param_sum, param_avg, param_min, param_max, param_minmaxconcat, param_allconcat, param_vecxtfidf = [False, False, False, False, False, False, False, True]  # Choose aggregation method
#         X_doc, X_sum, X_avg, X_min, X_max, X_minmaxconcat, X_allconcat, X_vecxtfidf = [[] for _ in range(8)]
        # Note: X_doc and X_avg both providing mean of feature vectors with slightly change in values    
        X = list()       
        
        if aggregation == 'TF-IDF Weightage':            # Getting TF-IDF word weights if param_vecxtfidf is True 
            if pageInfo == 'WFTeachTest':
                vectorizer_tfidf=utils.load_vectorizer(correlationId,'TF-IDF Weightage')
                df_tfidf = vectorizer_tfidf.transform(text)  
            else:
                vectorizer_tfidf=TfidfVectorizer(ngram_range=n_gram)
                df_tfidf = vectorizer_tfidf.fit_transform(text) 
                utils.save_vectorizer(correlationId,'TF-IDF Weightage',vectorizer_tfidf)
            #check for whatif
        vectorizer1=CountVectorizer(ngram_range=n_gram)  # used to get ngram word combinations
        if vec=='Glove':
                if language =='english':
                    spacy_vectors = spacy.load('en_core_web_lg')
                elif language =='spanish':
                    spacy_vectors = spacy.load('es_core_news_lg')
                elif language =='portuguese':
                    spacy_vectors = spacy.load('pt_core_news_lg')
                elif language =='german':
                    spacy_vectors = spacy.load('de_core_news_lg')
                elif language =='chinese':
                    spacy_vectors = spacy.load('zh_core_web_lg')
                elif language =='japanese':
                    spacy_vectors = spacy.load('ja_core_news_lg')
                elif language =='french':
                    spacy_vectors = spacy.load('fr_core_news_lg')
                elif language =='thai':
                    spacy_vectors = fasttext.load_model(thai_model_path)
        for i, sent in enumerate(text):
        #     tokens = sent.split()
            vectorizer1.fit_transform([sent])
            tokens = vectorizer1.get_feature_names()

            ### Getting word embeddings based on selected vectorizer ###
            if vec=='Glove':
                if language !='thai':
                    sent_vecs = [spacy_vectors(word).vector for word in tokens] # Getting glove vectors for each word in a sentence
                else:
                    sent_vecs =  [spacy_vectors[word] for word in tokens]
            if vec=='Word2Vec':
                w2v_model = Word2Vec([tokens],min_count=1)
                sent_vecs = w2v_model[w2v_model.wv.vocab]

            ### Running selected aggregation technique ###
    #         if param_doc:
    #             temp_doc = spacy_vectors(sent).vector  # Doc level vectors using raw sentence string
    #             X.append(temp_doc)

            if aggregation == 'Sum':
                temp_sum = np.sum(sent_vecs, axis=0)  # Simple addition of word vectors 
                X.append(temp_sum)

            if aggregation == 'Mean':
                temp_avg = np.average(sent_vecs, axis=0)
                X.append(temp_avg) # Averaging summed vectors

            if aggregation == 'Min':
                temp_min = np.minimum.reduce(sent_vecs)
                X.append(temp_min)  # Minimum values coordinate wise

            if aggregation == 'Max':
                temp_max = np.maximum.reduce(sent_vecs)
                X.append(temp_max)  # Maximum values coordinate wise

            if aggregation == 'Min-Max Concat':
                temp_min = np.minimum.reduce(sent_vecs)
                temp_max = np.maximum.reduce(sent_vecs)
                X.append(np.concatenate((temp_min, temp_max), axis=0))  # Concat of min, max

    #         if param_allconcat:
    #             X_allconcat.append(np.concatenate((temp_sum, temp_avg, temp_min, temp_max), axis=0))   # Concat of all vectors

            if aggregation == 'TF-IDF Weightage':
                # Multiplying TF-IDF with word vectors and averaging to sentence 
    #             vectorizer1.fit_transform(sent.split())
                feature_index = [vectorizer_tfidf.vocabulary_[w] if w in vectorizer_tfidf.get_feature_names() else 0 for w in vectorizer1.get_feature_names()]
                tfidf_scores = [df_tfidf[i, x] for x in feature_index]
                temp_multiply = np.array([np.multiply(sent_vecs[i], w) for i, w in enumerate(tfidf_scores)])
                temp_vecxtfidf = np.average(temp_multiply, axis=0)    
                X.append(temp_vecxtfidf)
            
        vectorized_df = pd.DataFrame(X)
            
    return vectorized_df

def check_ngram(text, n_gram):
    
    vectorizer1=CountVectorizer(ngram_range=n_gram)
    indexes_to_drop = []
    for i, sent in enumerate(text):
        try:
            vectorizer1.fit_transform(pd.Series(sent))
        except Exception:
            indexes_to_drop.append(i)
    percentage = len(indexes_to_drop)/len(text)
    if percentage<0.05:        
        return False, indexes_to_drop
    else:
        return True, indexes_to_drop
        
def vectorize(sent,number,vec,aggregation,vectorizer_tfidf,df_tfidf,language):
    #print(number)
    vectorizer1.fit_transform([sent])
    tokens = vectorizer1.get_feature_names()
    #print(number)
    ### Getting word embeddings based on selected vectorizer ###
    if vec=='Glove':
        if language !='thai':
            sent_vecs = [spacy_vectors(word).vector for word in tokens] # Getting glove vectors for each word in a sentence
        else:
            sent_vecs =  [spacy_vectors[word] for word in tokens]
    if vec=='Word2Vec':
        w2v_model = Word2Vec([tokens],min_count=1)
        sent_vecs = w2v_model[w2v_model.wv.vocab]


    if aggregation == 'Sum':
        temp_sum = np.sum(sent_vecs, axis=0)  # Simple addition of word vectors 
        #X.append(temp_sum)
        #return number,temp_sum
        return temp_sum

    if aggregation == 'Mean':
        temp_avg = np.average(sent_vecs, axis=0)
        #X.append(temp_avg) # Averaging summed vectors
        return temp_avg

    if aggregation == 'Min':
        temp_min = np.minimum.reduce(sent_vecs)
        #X.append(temp_min)  # Minimum values coordinate wise
        return temp_min
        
    if aggregation == 'Max':
        temp_max = np.maximum.reduce(sent_vecs)
        #X.append(temp_max)  # Maximum values coordinate wise
        return temp_max

    if aggregation == 'Min-Max Concat':
        temp_min = np.minimum.reduce(sent_vecs)
        temp_max = np.maximum.reduce(sent_vecs)
        #X.append(np.concatenate((temp_min, temp_max), axis=0))  # Concat of min, max
        return np.concatenate((temp_min, temp_max), axis=0)
   
    if aggregation == 'TF-IDF Weightage':
        feature_index = [vectorizer_tfidf.vocabulary_[w] if w in vectorizer_tfidf.get_feature_names() else 0 for w in vectorizer1.get_feature_names()]
        tfidf_scores = [df_tfidf[number, x] for x in feature_index]
        temp_multiply = np.array([np.multiply(sent_vecs[i], w) for i, w in enumerate(tfidf_scores)])
        temp_vecxtfidf = np.average(temp_multiply, axis=0)    
        #X.append(temp_vecxtfidf)
        return temp_vecxtfidf
   
    
def clustering_optional_multithread(text, correlationId, pageInfo = 'DataPreprocessing',language ='english'):
    '''
    Creates document level word vectors for Count Vectorizer and Tfidf Vectorizer
    Creates document level aggregation of multidimentional word features for Word2Vec and Glove
    '''
    EnDeRequired = utils.getEncryptionFlag(correlationId)
    #if language=='french':
    #    language='english'
    dbproconn,dbprocollection = utils.open_dbconn("DE_DataProcessing")
    data_json = list(dbprocollection.find({"CorrelationId" :correlationId}))
    if EnDeRequired :
        t = base64.b64decode(data_json[0].get('DataModification'))
        data_json[0]['DataModification']  =  eval(EncryptData.DescryptIt(t))
    text_dict= data_json[0].get('DataModification',{}).get('TextDataPreprocessing')
    vec=text_dict.get("Feature_Generator")
    n_gram=tuple(text_dict.get("N-grams"))
    aggregation=text_dict.get("Aggregation")
    if vec=='Count Vectorizer':
        if pageInfo == 'WFTeachTest':
            vectorizer=utils.load_vectorizer(correlationId,'Count Vectorization')
            X = vectorizer.transform(text) 
            vectorized_df = pd.DataFrame(X.toarray()) 
        else:
            vectorizer=CountVectorizer(ngram_range=n_gram)
            X = vectorizer.fit_transform(text) 
            vectorized_df = pd.DataFrame(X.toarray())
            utils.save_vectorizer(correlationId,'Count Vectorization',vectorizer)
    
    if vec=='Tfidf Vectorizer':
        if pageInfo == 'WFTeachTest':
            vectorizer=utils.load_vectorizer(correlationId,'Tfidf Vectorizer')
            X = vectorizer.transform(text) 
            vectorized_df = pd.DataFrame(X.toarray()) 
        else:
            vectorizer=TfidfVectorizer(ngram_range=n_gram)
            X = vectorizer.fit_transform(text) 
            vectorized_df = pd.DataFrame(X.toarray())        
            utils.save_vectorizer(correlationId,'Tfidf Vectorizer',vectorizer)
    
    
    if vec=='Word2Vec' or vec=='Glove':
        
#         glv, w2v  = True, False  # Choose vectorization technique
        #param_doc, param_sum, param_avg, param_min, param_max, param_minmaxconcat, param_allconcat, param_vecxtfidf = [False, False, False, False, False, False, False, True]  # Choose aggregation method
#         X_doc, X_sum, X_avg, X_min, X_max, X_minmaxconcat, X_allconcat, X_vecxtfidf = [[] for _ in range(8)]
        # Note: X_doc and X_avg both providing mean of feature vectors with slightly change in values    
        X = list()       
        vectorizer_tfidf = None
        df_tfidf = None
        if aggregation == 'TF-IDF Weightage':            # Getting TF-IDF word weights if param_vecxtfidf is True 
            if pageInfo == 'WFTeachTest':
                vectorizer_tfidf=utils.load_vectorizer(correlationId,'TF-IDF Weightage')
                df_tfidf = vectorizer_tfidf.transform(text)  
            else:
                vectorizer_tfidf=TfidfVectorizer(ngram_range=n_gram)
                df_tfidf = vectorizer_tfidf.fit_transform(text) 
                utils.save_vectorizer(correlationId,'TF-IDF Weightage',vectorizer_tfidf)
            #check for whatif
        global vectorizer1
        vectorizer1=CountVectorizer(ngram_range=n_gram)  # used to get ngram word combinations
        global spacy_vectors
        if vec=='Glove':
                if language =='english':
                    
                    spacy_vectors = spacy.load('en_core_web_lg') 
                elif language =='spanish':
                    spacy_vectors = spacy.load('es_core_news_lg')
                elif language =='portuguese':
                    spacy_vectors = spacy.load('pt_core_news_lg')
                elif language =='german':
                    spacy_vectors = spacy.load('de_core_news_lg')
                elif language =='chinese':
                    spacy_vectors = spacy.load('zh_core_web_lg')
                elif language =='japanese':
                    spacy_vectors = spacy.load('ja_core_news_lg')
                elif language =='french':
                    spacy_vectors = spacy.load('fr_core_news_lg')
                elif language =='thai':
                    spacy_vectors = fasttext.load_model(thai_model_path)
        
        if platform.system() == 'Linux':
            X.append(Parallel(n_jobs=3,backend='multiprocessing')(
                delayed(vectorize)(text[i],i,vec,aggregation,vectorizer_tfidf,df_tfidf,language) for i in range(0,len(text))))
        elif platform.system() == 'Windows':
            X.append(Parallel(n_jobs=3,prefer= 'threads')(
                delayed(vectorize)(text[i],i,vec,aggregation,vectorizer_tfidf,df_tfidf,language) for i in range(0,len(text))))
            
        vectorized_df = pd.DataFrame(X[0])
        #print("sequence:: ", list(vectorized_df[0]))    
    return vectorized_df