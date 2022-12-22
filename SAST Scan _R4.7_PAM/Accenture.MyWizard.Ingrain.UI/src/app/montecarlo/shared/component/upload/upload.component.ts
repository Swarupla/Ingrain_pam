import { Component, OnInit, EventEmitter, Output, Input, ViewChild } from '@angular/core';

@Component({
  selector: 'app-upload',
  templateUrl: './upload.component.html',
  styleUrls: ['./upload.component.scss']
})
export class UploadComponent implements OnInit {
  @ViewChild('fileInputRef', { static: false }) fileInputRef; 
  @Output() template = new EventEmitter();
  @Output() fileData = new EventEmitter();
  @Input() flow; // 'generic' , 'notgeneric'
  @Input() initiator; // 'Input', 'Landing'
  private file = [];
  constructor() { }
  ngOnInit() {
  }

  private getFileDetails(e) {
    this.file = [];
    this.file.push(e.target.files[0]);
    this.fileData.emit(this.file);

  }

  private setTemplate(name) {
    // this.template = name;
    this.template.emit('MappingPhases');
  }

  allowDrop(event) {
    event.preventDefault();
  }

  onDrop(event) {
    event.preventDefault();
    this.file = [];
    this.file.push(event.dataTransfer.files[0]);
    this.fileData.emit(this.file);
  }

  removeFile() {
    this.fileInputRef.nativeElement.value = '';
    this.file = [];
    this.fileData.emit(this.file);
  }
}
