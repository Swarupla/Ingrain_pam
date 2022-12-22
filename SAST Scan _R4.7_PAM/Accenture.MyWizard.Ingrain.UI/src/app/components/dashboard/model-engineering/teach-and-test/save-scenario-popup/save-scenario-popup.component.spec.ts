import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SaveScenarioPopupComponent } from './save-scenario-popup.component';

describe('SaveScenarioPopupComponent', () => {
  let component: SaveScenarioPopupComponent;
  let fixture: ComponentFixture<SaveScenarioPopupComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SaveScenarioPopupComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SaveScenarioPopupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
