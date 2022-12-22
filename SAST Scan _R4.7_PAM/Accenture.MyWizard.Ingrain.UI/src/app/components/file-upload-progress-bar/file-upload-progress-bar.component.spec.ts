import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { FileUploadProgressBarComponent } from './file-upload-progress-bar.component';

describe('FileUploadProgressBarComponent', () => {
  let component: FileUploadProgressBarComponent;
  let fixture: ComponentFixture<FileUploadProgressBarComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ FileUploadProgressBarComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(FileUploadProgressBarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
