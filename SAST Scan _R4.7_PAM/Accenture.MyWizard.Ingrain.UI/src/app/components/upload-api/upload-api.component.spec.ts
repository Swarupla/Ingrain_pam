import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { UploadApiComponent } from './upload-api.component';

describe('UploadApiComponent', () => {
  let component: UploadApiComponent;
  let fixture: ComponentFixture<UploadApiComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ UploadApiComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(UploadApiComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
