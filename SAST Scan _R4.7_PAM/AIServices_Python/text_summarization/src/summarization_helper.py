# Created by Joydeep Sarkar
# Created on 09-Feb-2018
# Copyright MyWizard VA

import numpy as np
import pandas as pd
import nltk
import sys
import networkx as nx
from nltk.corpus import stopwords
from nltk.tokenize import sent_tokenize
from sklearn.metrics.pairwise import cosine_similarity
import re
import os

from pathlib import Path
currentDirectory = str(Path(__file__).parent.absolute())
currentDirectory_glove = str(Path(__file__).parent.parent.absolute())
from shared.logger_helper import GenerateLogger
log_generator = GenerateLogger()
currentDirectory_logger = str(Path(__file__).parent.parent.absolute())
logger = log_generator.generate_logger(currentDirectory_logger + "/summarization.log")

alphabets= "([A-Za-z])"
prefixes = "(Mr|St|Mrs|Ms|Dr)[.]"
suffixes = "(Inc|Ltd|Jr|Sr|Co)"
starters = "(Mr|Mrs|Ms|Dr|He\s|She\s|It\s|They\s|Their\s|Our\s|We\s|But\s|However\s|That\s|This\s|Wherever)"
acronyms = "([A-Z][.][A-Z][.](?:[A-Z][.])?)"
websites = "[.](com|net|org|io|gov)"

class SummarizationHelper:

    def __init__(self):
        
        self._glove_data_path = currentDirectory_glove +'/glove.txt'

        
        

    def remove_stopwords(self, sen, wrd):
        sen_new = " ".join([i for i in sen if i not in wrd])
        return sen_new
    def split_into_sentences(self, text):
        text = " " + text + "  "
        text = text.replace("\n"," ")
        text = re.sub(prefixes,"\\1<prd>",text)
        text = re.sub(websites,"<prd>\\1",text)
        if "Ph.D" in text: text = text.replace("Ph.D.","Ph<prd>D<prd>")
        text = re.sub("\s" + alphabets + "[.] "," \\1<prd> ",text)
        text = re.sub(acronyms+" "+starters,"\\1<stop> \\2",text)
        text = re.sub(alphabets + "[.]" + alphabets + "[.]" + alphabets + "[.]","\\1<prd>\\2<prd>\\3<prd>",text)
        text = re.sub(alphabets + "[.]" + alphabets + "[.]","\\1<prd>\\2<prd>",text)
        text = re.sub(" "+suffixes+"[.] "+starters," \\1<stop> \\2",text)
        text = re.sub(" "+suffixes+"[.]"," \\1<prd>",text)
        text = re.sub(" " + alphabets + "[.]"," \\1<prd>",text)
        #if """ in text: text = text.replace("."","".")
        if "\"" in text: text = text.replace(".\"","\".")
        if "!" in text: text = text.replace("!\"","\"!")
        if "?" in text: text = text.replace("?\"","\"?")
        text = text.replace(".",".<stop>")
        text = text.replace("?","?<stop>")
        text = text.replace("!","!<stop>")
        text = text.replace("<prd>",".")
        sentences = text.split("<stop>")
        sentences = sentences[0:1]
        sentences = [s.strip() for s in sentences]
        return sentences[0]

    def get_summary(self, text, limit):
        
        sentences = []
        sum_list = []
        return_value = {}
        return_value["is_success"] = False
        return_value["message"] = ""
        return_value["response_data"] = []
        try:
            text=text.replace('  ','')
            text="  ".join(text.split())
            print("text----",text)
            for line in text.split('\n'):
                sentences.append(sent_tokenize(line))
            sentences = [y for x in sentences for y in x]  # flatten list
            # remove punctuations, numbers and special characters
            clean_sentences = pd.Series(sentences).str.replace("[^a-zA-Z]", " ")
            # make alphabets lowercase
            clean_sentences = [s.lower() for s in clean_sentences]
            stop_words = stopwords.words('english')
            # remove stopwords from the sentences
            clean_sentences = [self.remove_stopwords(r.split(), stop_words) for r in clean_sentences]
            # Extract word vectors
            word_embeddings = {}
            f = open(self._glove_data_path, encoding='utf-8')
            for line in f:
                values = line.split()
                word = values[0]
                coefs = np.asarray(values[1:], dtype='float32')
                word_embeddings[word] = coefs
            f.close()
            sentence_vectors = []
            for i in clean_sentences:
                if len(i) != 0:
                    v = sum([word_embeddings.get(w, np.zeros((100,))) for w in i.split()]) / (len(i.split()) + 0.001)
                else:
                    v = np.zeros((100,))
                sentence_vectors.append(v)
            # similarity matrix
            sim_mat = np.zeros([len(sentences), len(sentences)])
            for i in range(len(sentences)):
                for j in range(len(sentences)):
                    if i != j:
                        sim_mat[i][j] = cosine_similarity(sentence_vectors[i].reshape(1, 100), sentence_vectors[j].reshape(1, 100))[0, 0]
            # Applying Page Rank Algorithm
            nx_graph = nx.from_numpy_array(sim_mat)
            scores = nx.pagerank(nx_graph)
            ranked_sentences = sorted(((scores[i], s) for i, s in enumerate(sentences)), reverse=True)
            length = len(ranked_sentences)
            
            
            l=0
            var = limit
            
            if var == 0:
                l = int(length * 0.50)
                list2 = []
                for i in range(l):
                    list2.append(ranked_sentences[i][1])
                raw = text.split('\n')
                sum_list = []
                for i in list2:
                    for j in range(len(raw)):
                        if i in raw[j]:
                            slice = ''.join(raw[j].split('.')[:2])
                            sum_list.append({j: i})
                summ_dict = {}
                for i in sum_list:
                    for k, v in i.items():
                        if k not in summ_dict.keys():

                            summ_dict[k] = [v]
                        else:
                            summ_dict[k].append(v)
                summ_key_list = list(summ_dict.keys())
                summ_key_list.sort()
                data = [{"summary": ''.join(summ_dict[i])} for i in summ_key_list]
                return {"is_success": True, "message": "", "response_data": data}
            elif int(length * 0.20) < var <= int(length * 0.95):
                
                l=var
                list2 = []
                for i in range(l):
                    list2.append(ranked_sentences[i][1])
                
                raw = text.split('\n')
                
                sum_list = []
                list3 = []
                for i in range(len(list2)):
                    list3.append(self.split_into_sentences(list2[i]))
                list2 = list3
                for i in list2:
                    for j in range(len(raw)):
                        if i in raw[j]:
                            slice = ''.join(raw[j].split('.')[:2])
                            sum_list.append({j: i})
                summ_dict = {}
                for i in sum_list:
                    for k, v in i.items():
                        if k not in summ_dict.keys():
                            summ_dict[k] = [v]
                        else:
                            summ_dict[k].append(v)
                summ_key_list = list(summ_dict.keys())
                summ_key_list.sort()
                data = [{"summary": ''.join(summ_dict[i])} for i in summ_key_list]
                return_value["is_success"] = True
                return_value["message"] = ""
                return_value["response_data"] = data
            else:
                return_value["is_success"] = False
                return_value["message"] = "Please re-adjust the maximum number of sentence and try again."
                return_value["response_data"] = []
        except Exception as e:
            return_value["is_success"] = False
            return_value["message"] = str(e)
            return_value["response_data"] = []
            logger.error(str(e))

        return return_value
		
if __name__ == "__main__": 
    returnValue = {}
    returnValue["is_success"] = ""
    returnValue["message"] = ""
    returnValue["response_data"] = ""
    #query = "Accenture plc, stylised as accenture, is an Irish-domiciled multinational professional services company that provides services in strategy, consulting, digital, technology and operations. A Fortune Global 500 company,[4] it has been incorporated in Dublin, Ireland since September 1, 2009. In 2019, the company reported revenues of $43.2 billion, with more than 492,000 employees[2] serving clients in more than 200 cities in 120 countries.[5] In 2015, the company had about 150,000 employees in India,[6] 48,000 in the US,[7] and 50,000 in the Philippines.[8] Accenture's current clients include 91 of the Fortune Global 100 and more than three-quarters of the Fortune Global 500. Accenture is a multi-national company. Its CEO is Julie Sweet. It has over 5 million employees. Its headquarter in India isw Bangalore. It supports all communities and lgbt."
    query = "The Prime Minister participated in an online inauguration from New Delhi.He said that the government had taken steps to protect those on duty.Due to mob mentality, those on duty doctors, nurses and other healthcare workers are subjected to violence. The Prime Minister also said that insurance cover of 50 lakh had been provided to those on the frontline. The Prime Minister also said that there was a need to focus on humanity centric development in the light of the COVID19 pandemic. Stressing that this was the biggest crisis since the Second World War, Mr. Modi said that the pre and post COVID19 world would be different. He said that he was confident that the medical workers were sure to win in this battle. Large MNC companies like Accenture are constantly producing internal knowledge, which frequently gets stored and under used in databases as unstructured data. Summarization can enable analysts to quickly understand everything the company has already done in a given subject, and quickly assemble reports that incorporate different points of view.In our daily office activities we received lots of mail and to read all the mail one by one will be much time consuming. So, Summarization can quickly update the content by producing summarized reports on their subject of interest."
    range1 = 5
    summarization = SummarizationHelper()
    returnValue = summarization.get_summary(query, range1)
    print(returnValue)