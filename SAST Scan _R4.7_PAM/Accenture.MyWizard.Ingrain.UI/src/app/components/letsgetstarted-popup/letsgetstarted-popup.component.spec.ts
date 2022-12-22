import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { LetsgetstartedPopupComponent } from './letsgetstarted-popup.component';

describe('LetsgetstartedPopupComponent', () => {
  let component: LetsgetstartedPopupComponent;
  let fixture: ComponentFixture<LetsgetstartedPopupComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ LetsgetstartedPopupComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(LetsgetstartedPopupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
