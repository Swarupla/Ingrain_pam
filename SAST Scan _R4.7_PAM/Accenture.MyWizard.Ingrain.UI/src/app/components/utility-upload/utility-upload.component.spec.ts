import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { UtilityUploadComponent } from './utility-upload.component';

describe('UtilityUploadComponent', () => {
  let component: UtilityUploadComponent;
  let fixture: ComponentFixture<UtilityUploadComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ UtilityUploadComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(UtilityUploadComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
