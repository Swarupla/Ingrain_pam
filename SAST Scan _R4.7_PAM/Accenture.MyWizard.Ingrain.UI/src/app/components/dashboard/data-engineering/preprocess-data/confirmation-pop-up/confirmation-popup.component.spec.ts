import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ConfirmationPopUpComponent } from './confirmation-popup.component';

describe('ConfirmationPopUpComponent', () => {
  let component: ConfirmationPopUpComponent;
  let fixture: ComponentFixture<ConfirmationPopUpComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ConfirmationPopUpComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ConfirmationPopUpComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
