import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ValidRecordDetailsPopupComponent } from './valid-record-details-popup.component';

describe('ValidRecordDetailsPopupComponent', () => {
  let component: ValidRecordDetailsPopupComponent;
  let fixture: ComponentFixture<ValidRecordDetailsPopupComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ValidRecordDetailsPopupComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ValidRecordDetailsPopupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
